-- 1) Enable PostGIS
CREATE EXTENSION IF NOT EXISTS postgis;

-- 2) Identity tables
CREATE TABLE "AspNetRoles" (
  "Id"               VARCHAR(450)    NOT NULL PRIMARY KEY,
  "Name"             VARCHAR(256),
  "NormalizedName"   VARCHAR(256)    UNIQUE,
  "ConcurrencyStamp" VARCHAR(256)
);

CREATE TABLE "AspNetUsers" (
  "Id"                   VARCHAR(450)   NOT NULL PRIMARY KEY,
  "UserName"             VARCHAR(256),
  "NormalizedUserName"   VARCHAR(256)   UNIQUE,
  "Email"                VARCHAR(256),
  "NormalizedEmail"      VARCHAR(256),
  "EmailConfirmed"       BOOLEAN        NOT NULL DEFAULT FALSE,
  "PasswordHash"         TEXT,
  "SecurityStamp"        TEXT,
  "ConcurrencyStamp"     TEXT,
  "PhoneNumber"          TEXT,
  "PhoneNumberConfirmed" BOOLEAN        NOT NULL DEFAULT FALSE,
  "TwoFactorEnabled"     BOOLEAN        NOT NULL DEFAULT FALSE,
  "LockoutEnd"           TIMESTAMP WITH TIME ZONE,
  "LockoutEnabled"       BOOLEAN        NOT NULL DEFAULT FALSE,
  "AccessFailedCount"    INTEGER        NOT NULL DEFAULT 0
);

CREATE TABLE "AspNetRoleClaims" (
  "Id"      SERIAL       NOT NULL PRIMARY KEY,
  "RoleId"  VARCHAR(450) NOT NULL,
  "ClaimType"  TEXT,
  "ClaimValue" TEXT,
  FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
  "UserId" VARCHAR(450) NOT NULL,
  "RoleId" VARCHAR(450) NOT NULL,
  PRIMARY KEY ("UserId","RoleId"),
  FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
  FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
  "Id"      SERIAL       NOT NULL PRIMARY KEY,
  "UserId"  VARCHAR(450) NOT NULL,
  "ClaimType"  TEXT,
  "ClaimValue" TEXT,
  FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
  "LoginProvider" VARCHAR(450) NOT NULL,
  "ProviderKey"   VARCHAR(450) NOT NULL,
  "ProviderDisplayName" TEXT,
  "UserId"        VARCHAR(450) NOT NULL,
  PRIMARY KEY ("LoginProvider","ProviderKey"),
  FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
  "UserId"        VARCHAR(450) NOT NULL,
  "LoginProvider" VARCHAR(450) NOT NULL,
  "Name"          VARCHAR(450) NOT NULL,
  "Value"         TEXT,
  PRIMARY KEY ("UserId","LoginProvider","Name"),
  FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

-- 3) Domain tables (preserve your original schema + link to AspNetUsers)

-- Critic profile
CREATE TABLE "Critic" (
    "CriticId"    SERIAL        NOT NULL PRIMARY KEY,
    "UserId"      VARCHAR(450)  NOT NULL,
    "Username"    VARCHAR(255)  NOT NULL UNIQUE,
    "FirstName"   VARCHAR(255)  NOT NULL,
    "LastName"    VARCHAR(255)  NOT NULL,
    "Email"       VARCHAR(100)  NOT NULL UNIQUE,
    "Food"        INTEGER       NOT NULL DEFAULT 5,
    "Service"     INTEGER       NOT NULL DEFAULT 4,
    "Price"       INTEGER       NOT NULL DEFAULT 3,
    "Condition"   INTEGER       NOT NULL DEFAULT 2,
    "Atmosphere"  INTEGER       NOT NULL DEFAULT 1,
    "CreatedAt"   TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

-- Entrepreneur profile
CREATE TABLE "Entrepreneur" (
    "EntrepreneurId"  SERIAL        NOT NULL PRIMARY KEY,
    "UserId"          VARCHAR(450)  NOT NULL,
    "Username"        VARCHAR(255)  NOT NULL UNIQUE,
    "FirstName"       VARCHAR(255)  NOT NULL,
    "LastName"        VARCHAR(255)  NOT NULL,
    "Email"           VARCHAR(255)  NOT NULL UNIQUE,
    "CreatedAt"       TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

-- Admin profile
CREATE TABLE "Admin" (
    "AdminId"    SERIAL        NOT NULL PRIMARY KEY,
    "UserId"     VARCHAR(450)  NOT NULL,
    "Username"   VARCHAR(255)  NOT NULL UNIQUE,
    "FirstName"  VARCHAR(255)  NOT NULL,
    "LastName"   VARCHAR(255)  NOT NULL,
    "Email"      VARCHAR(255)  NOT NULL UNIQUE,
    "CreatedAt"  TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

-- Restaurant, Review, and Image tables
CREATE TABLE "Restaurant" (
    "RestaurantId"    SERIAL       NOT NULL PRIMARY KEY,
    "EntrepreneurId"  INTEGER      NOT NULL,
    "Name"            VARCHAR(255) NOT NULL,
    "Website"         TEXT,
    "Location"        geography    NOT NULL,
    "Seats"           INTEGER      NOT NULL,
    "RestaurantType"  VARCHAR(255) NOT NULL,
    "AverageRating"   DOUBLE PRECISION NOT NULL DEFAULT 0,
    "ReviewCount"     INTEGER      NOT NULL DEFAULT 0,
    "CreatedAt"       TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("EntrepreneurId") REFERENCES "Entrepreneur"("EntrepreneurId")
);

CREATE TABLE "RestaurantImage" (
    "ImageId"       SERIAL    NOT NULL PRIMARY KEY,
    "RestaurantId"  INTEGER   NOT NULL,
    "ImageData"     BYTEA     NOT NULL,
    "CreatedAt"     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RestaurantId") REFERENCES "Restaurant"("RestaurantId")
);

CREATE TABLE "Review" (
    "ReviewId"     SERIAL            NOT NULL PRIMARY KEY,
    "RestaurantId" INTEGER           NOT NULL,
    "CriticId"     INTEGER           NOT NULL,
    "Food"         NUMERIC(3,2)      NOT NULL CHECK ("Food" BETWEEN 0.00 AND 5.00),
    "Service"      NUMERIC(3,2)      NOT NULL CHECK ("Service" BETWEEN 0.00 AND 5.00),
    "Price"        NUMERIC(3,2)      NOT NULL CHECK ("Price" BETWEEN 0.00 AND 5.00),
    "Condition"    NUMERIC(3,2)      NOT NULL CHECK ("Condition" BETWEEN 0.00 AND 5.00),
    "Atmosphere"   NUMERIC(3,2)      NOT NULL CHECK ("Atmosphere" BETWEEN 0.00 AND 5.00),
    "Average"      NUMERIC(3,2),
    "Comment"      TEXT              NOT NULL,
    "CreatedAt"    TIMESTAMP         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RestaurantId") REFERENCES "Restaurant"("RestaurantId"),
    FOREIGN KEY ("CriticId")     REFERENCES "Critic"("CriticId")
);

-- 4) Trigger for updating averages
CREATE OR REPLACE FUNCTION update_restaurant_average()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    cnt            integer;
    avg_val        numeric;
    sigmoid_factor numeric;
BEGIN
    -- Compute count and raw average of per-review averages
    SELECT COUNT(*), AVG("Average")
      INTO cnt, avg_val
    FROM "Review"
    WHERE "RestaurantId" = NEW."RestaurantId";

    -- Plain logistic (0 < factor <= 1)
    sigmoid_factor := 1.0 / (1.0 + exp(-cnt));

    -- Update restaurant, scaling average then clamping to 5.0
    UPDATE "Restaurant"
    SET
      "AverageRating" = LEAST(avg_val * sigmoid_factor, 5.0),
      "ReviewCount"   = cnt
    WHERE "RestaurantId" = NEW."RestaurantId";

    RETURN NEW;
END;
$$;
CREATE TRIGGER trg_update_restaurant_avg
  AFTER INSERT ON "Review"
  FOR EACH ROW
  EXECUTE FUNCTION update_restaurant_average();
