using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace PT
{
    public class PTContext : DbContext
    {
        public DbSet<User> User { get; set; }
        public DbSet<UserType> UserType { get; set; }

        // Required for migrations to work
        public PTContext(DbContextOptions<PTContext> contextOptions) : base(contextOptions)
        {
            Database.EnsureCreated();
        }
    }

    [Table("users")]
    public class User
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("UserId")]
        public string UserId { get; internal set; }

        [Column("Username")]
        public string Username { get; internal set; }

        [Column("Email")]
        public string Email { get; internal set; }

        [Column("Password")]
        public string Password { get; internal set; }

        [Column("UserTypeId")]
        public int UserTypeId { get; internal set; }

        [Column("IsLoggedIn")]
        public bool IsLoggedIn { get; internal set; }
    }

    [Table("userTypes")]
    public class UserType
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("UserTypeId")]
        public int UserTypeId { get; internal set; }

        [Column("UserTypeName")]
        public string UserTypeName { get; internal set; }
    }
}
