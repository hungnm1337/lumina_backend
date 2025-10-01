using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.User
{
    public class UserDto
    {
        public int UserId { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public string? UserName { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Bio {  get; set; }

        public string? Phone { get; set; }

        public bool? IsActive { get; set; }

        public string? JoinDate { get; set; }
    }
}
