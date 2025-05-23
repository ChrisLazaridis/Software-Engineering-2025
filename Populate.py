#!/usr/bin/env python3
import os
import psycopg2
import googlemaps
import random
import string
import datetime
from dotenv import load_dotenv
import time
import logging
import base64
import hashlib
import struct
import secrets
import uuid

# ─── Logging ────────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s %(levelname)s %(message)s',
    handlers=[
        logging.FileHandler("restaurant_import.log"),
        logging.StreamHandler()
    ]
)

# ─── Env & Clients ─────────────────────────────────────────────────────────
load_dotenv()
GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY")
gmaps = googlemaps.Client(key=GOOGLE_API_KEY)

DB_HOST = os.getenv("DB_HOST")
DB_NAME = os.getenv("DB_NAME")
DB_USER = os.getenv("DB_USER")
DB_PASS = os.getenv("DB_PASSWORD")

# ─── Constants ─────────────────────────────────────────────────────────────
NUM_ENTREPRENEURS = 30
RESTAURANT_TYPES = [
    "Italian", "Greek", "Chinese", "Fast Food", "Fine Dining",
    "Café", "Taverna", "Seafood"
]

# ─── Helpers ────────────────────────────────────────────────────────────────
def create_db_connection():
    return psycopg2.connect(
        host=DB_HOST, database=DB_NAME,
        user=DB_USER, password=DB_PASS
    )
import base64
import hashlib
import secrets
import struct
import subprocess


def identityv3_hash_password(password: str) -> str:
    """
    Calls the .NET HashTool console app to get a 
    V3-format Identity password hash blob.
    """
    # 1) Run HashTool; adjust the project path as needed
    proc = subprocess.run(
        ["dotnet", "run", "--project", r"HashTool", password],
        capture_output=True,    # capture both stdout/stderr
        text=True,              # decode bytes → str
        check=True              # raise if exit code ≠ 0
    )  

    # 2) Extract the line that starts with "Hash: "
    for line in proc.stdout.splitlines():
        if line.startswith("Hash:"):
            # remove the prefix and any surrounding whitespace
            return line[len("Hash:"):].strip()

    # 3) If no "Hash:" line, fall back to raw output
    return proc.stdout.strip()

def gen_password(length=10):
    alphabet = string.ascii_letters + string.digits
    # start with a random special letter
    password = random.choice(string.punctuation)
    password += ''.join(random.choice(alphabet) for _ in range(length))
    return password

def gen_name():
    first_names = ["Maria","Nikos","Eleni","Giorgos","Katerina","Dimitris","Anna","Kostas"]
    last_names  = ["Papadopoulos","Papanikolaou","Karagiannis","Vlachos","Makris"]
    return random.choice(first_names), random.choice(last_names)

def gen_email(fn, ln):
    domains = ["gmail.com","hotmail.com","yahoo.com"]
    return f"{fn.lower()}.{ln.lower()}{random.randint(1,9999)}@{random.choice(domains)}"

# ─── Reset ALL tables ───────────────────────────────────────────────────────
def reset_all(conn):
    cur = conn.cursor()
    logging.info("Dropping/truncating Identity and domain tables...")
    tables = [
        'AspNetUserTokens',
        'AspNetUserLogins',
        'AspNetUserClaims',
        'AspNetRoleClaims',
        'AspNetUserRoles',
        'Review',
        'RestaurantImage',
        'Restaurant',
        'Critic',
        'Entrepreneur',
        'Admin',
        'AspNetUsers',
        'AspNetRoles'
    ]
    for tbl in tables:
        cur.execute(f'TRUNCATE TABLE "{tbl}" RESTART IDENTITY CASCADE;')
    conn.commit()

# ─── Seed AspNetRoles ───────────────────────────────────────────────────────
def seed_roles(cur):
    roles = [("Critic","CRITIC"),("Entrepreneur","ENTREPRENEUR"),("Admin","ADMIN")]
    logging.info("Seeding AspNetRoles...")
    for name,norm in roles:
        rid = str(uuid.uuid4())
        stamp = str(uuid.uuid4())
        cur.execute(
            '''INSERT INTO "AspNetRoles"("Id","Name","NormalizedName","ConcurrencyStamp")
               VALUES(%s,%s,%s,%s);''',
            (rid, name, norm, stamp)
        )
    cur.connection.commit()

def load_role_ids(cur):
    cur.execute('SELECT "NormalizedName","Id" FROM "AspNetRoles";')
    return {norm: rid for norm, rid in cur.fetchall()}

# ─── Create Entrepreneurs & AspNetUsers ────────────────────────────────────
def create_entrepreneurs(conn, num):
    cur = conn.cursor()
    roles = load_role_ids(cur)
    creds = []
    logging.info(f"Creating {num} entrepreneurs...")
    for _ in range(num):
        fn, ln = gen_name()
        email = gen_email(fn, ln)
        username = f"{fn.lower()}_{ln.lower()}{random.randint(1,9999)}"
        pwd = gen_password()
        pwd_hash = identityv3_hash_password(pwd)
        uid = str(uuid.uuid4())
        now = datetime.datetime.now(datetime.timezone.utc)

        # AspNetUsers
        cur.execute(
            '''INSERT INTO "AspNetUsers"
               ("Id","UserName","NormalizedUserName","Email","NormalizedEmail",
                "EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp",
                "PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
               VALUES(%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s);''',
            (uid, username, username.upper(),
             email, email.upper(),
             True, pwd_hash,
             str(uuid.uuid4()), str(uuid.uuid4()),
             False, False, True, 0)
        )
        # Profile
        cur.execute(
            '''INSERT INTO "Entrepreneur"
               ("UserId","Username","FirstName","LastName","Email","CreatedAt")
               VALUES(%s,%s,%s,%s,%s,%s);''',
            (uid, username, fn, ln, email, now)
        )
        # Role link
        cur.execute(
            'INSERT INTO "AspNetUserRoles"("UserId","RoleId") VALUES(%s,%s);',
            (uid, roles["ENTREPRENEUR"])
        )
        creds.append((username,email,pwd))

    conn.commit()
    with open("entrepreneur_credentials.txt","w", encoding="utf-8", errors="ignore") as f:
        for u,e,p in creds:
            f.write(f"{u} | {e} | {p}\n")
    return creds

# ─── Fetch & Save Restaurants + Critics/Reviews ─────────────────────────────
def import_restaurants(conn):
    cur = conn.cursor()
    roles = load_role_ids(cur)
    critic_creds = []
    seen = set()
    count = skipped = 0

    # fetch entrepreneur IDs
    cur.execute('SELECT "EntrepreneurId" FROM "Entrepreneur";')
    entrepreneur_ids = [r[0] for r in cur.fetchall()]

    neighborhoods = [
        {"lat":37.9838,"lng":23.7275},
        {"lat":37.9715,"lng":23.7267},
    ]
    queries = ["restaurant","tavern","cafe","fine dining"]
    target = 40

    logging.info("Importing restaurants and reviews via Google Maps…")
    for loc in neighborhoods:
        if count >= target: break
        for q in queries:
            if count >= target: break

            try:
                places = gmaps.places_nearby(location=loc, radius=2000,
                                             type="restaurant", keyword=q)
            except Exception as ex:
                logging.error(f"Places API error for '{q}' at {loc}: {ex}")
                continue

            for place in places.get("results", []):
                name = place.get("name","Unknown")
                if name in seen:
                    skipped += 1
                    continue

                try:
                    seen.add(name)

                    # Insert Restaurant
                    ent_id = random.choice(entrepreneur_ids)
                    cur.execute(
                        '''INSERT INTO "Restaurant"
                           ("EntrepreneurId","Name","Website","Location","Seats","RestaurantType")
                           VALUES(%s,%s,%s,
                             ST_SetSRID(ST_MakePoint(%s,%s),4326),
                             %s,%s)
                           RETURNING "RestaurantId";''',
                        (
                            ent_id, name, place.get("website"),
                            place["geometry"]["location"]["lng"],
                            place["geometry"]["location"]["lat"],
                            random.randint(20,200),
                            random.choice(RESTAURANT_TYPES)
                        )
                    )
                    rid = cur.fetchone()[0]
                    conn.commit()
                    count += 1
                    logging.info(f"[{count}] Inserted restaurant '{name}' (ID={rid})")

                    # Insert Photo
                    photos = place.get("photos", [])
                    if photos:
                        try:
                            ref = photos[0]["photo_reference"]
                            img = b''.join(gmaps.places_photo(photo_reference=ref, max_width=2400))
                            cur.execute(
                                'INSERT INTO "RestaurantImage"("RestaurantId","ImageData") VALUES(%s,%s);',
                                (rid, psycopg2.Binary(img))
                            )
                            conn.commit()
                        except Exception as ex:
                            logging.warning(f"Photo save failed for '{name}': {ex}")

                    # Insert Reviews & Critics
                    details = {}
                    try:
                        details = gmaps.place(place_id=place["place_id"], fields=["reviews"])["result"]
                    except Exception as ex:
                        logging.warning(f"Failed to fetch details for '{name}': {ex}")

                    for rv in details.get("reviews", []):
                        author = rv.get("author_name","Anonymous")
                        rating = rv.get("rating",3.0)
                        # find or create critic
                        cur.execute('SELECT "CriticId","UserId" FROM "Critic" WHERE "Username"=%s;', (author,))
                        row = cur.fetchone()
                        if row:
                            cid, cuid = row
                        else:
                            fn, ln = author.split()[0], " ".join(author.split()[1:]) or "User"
                            em = gen_email(fn, ln)
                            un = f"{fn.lower()}_{random.randint(1,9999)}"
                            pwd = gen_password()
                            ph = identityv3_hash_password(pwd)
                            cuid = str(uuid.uuid4())
                            now = datetime.datetime.now(datetime.timezone.utc)

                            # AspNetUsers
                            cur.execute(
                                '''INSERT INTO "AspNetUsers"
                                   ("Id","UserName","NormalizedUserName","Email","NormalizedEmail",
                                    "EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp",
                                    "PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
                                   VALUES(%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s);''',
                                (cuid, un, un.upper(),
                                 em, em.upper(),
                                 True, ph,
                                 str(uuid.uuid4()), str(uuid.uuid4()),
                                 False, False, True, 0)
                            )
                            # Critic profile
                            cur.execute(
                                '''INSERT INTO "Critic"
                                   ("UserId","Username","FirstName","LastName","Email","CreatedAt")
                                   VALUES(%s,%s,%s,%s,%s,%s) RETURNING "CriticId";''',
                                (cuid, un, fn, ln, em, now)
                            )
                            cid = cur.fetchone()[0]
                            # Role link
                            cur.execute(
                                'INSERT INTO "AspNetUserRoles"("UserId","RoleId") VALUES(%s,%s);',
                                (cuid, roles["CRITIC"])
                            )
                            critic_creds.append((un, em, pwd))
                            conn.commit()

                        # Review insert
                        avg = rating
                        cur.execute(
                            '''INSERT INTO "Review"
                               ("RestaurantId","CriticId","Food","Service","Price","Condition","Atmosphere","Average","Comment")
                               VALUES(%s,%s,%s,%s,%s,%s,%s,%s,%s);''',
                            (rid, cid, avg, avg, avg, avg, avg, avg, rv.get("text",""))
                        )
                        conn.commit()

                except Exception as e:
                    logging.error(f"Failed to import '{name}': {e}")
                    conn.rollback()
                    continue

    # Dump critic credentials with UTF-8
    with open("critic_credentials.txt", "w", encoding="utf-8", errors="ignore") as f:
        for u, e, p in critic_creds:
            f.write(f"{u} | {e} | {p}\n")

    logging.info(f"Done: {count} restaurants imported, {skipped} duplicates skipped.")

# ─── Main ───────────────────────────────────────────────────────────────────
def main():
    logging.info("Starting import…")
    conn = create_db_connection()
    reset_all(conn)

    cur = conn.cursor()
    seed_roles(cur)

    create_entrepreneurs(conn, NUM_ENTREPRENEURS)
    import_restaurants(conn)

    conn.close()
    logging.info("All tables reset and repopulated.")

if __name__ == "__main__":
    main()
