// --- Models/Common/BaseModels.cs ---
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SoftEng2025.Models.Common
{
    // Marker for anything holding a reference to the ASP.NET Identity user
    public interface IUserAccount
    {
        string UserId { get; set; }
        IdentityUser User { get; set; }
    }

    // Common profile fields
    public interface IPerson : IUserAccount
    {
        string Username { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
    }

    // Anything with a CreatedAt timestamp
    public interface ITrackable
    {
        DateTime CreatedAt { get; set; }
    }

    // Generic “container” of images
    public interface IImageContainer<TImage>
    {
        ICollection<TImage> Images { get; set; }
    }

    // Generic “container” of reviews
    public interface IReviewable<TReview>
    {
        ICollection<TReview> Reviews { get; set; }
    }

    // Shared implementation for Admin, Critic, Entrepreneur
    public abstract class Person : IPerson, ITrackable
    {
        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }

        [Required, MaxLength(255)]
        public string Username { get; set; }

        [Required, MaxLength(255)]
        public string FirstName { get; set; }

        [Required, MaxLength(255)]
        public string LastName { get; set; }

        [Required, MaxLength(255), EmailAddress]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}