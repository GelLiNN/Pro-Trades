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
        public int UserId { get; internal set; }

        [Column("UserName")]
        public string UserName { get; internal set; }

        [Column("UserEmail")]
        public string UserEmail { get; internal set; }

        [Column("UserTypeId")]
        public int UserTypeId { get; internal set; }
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