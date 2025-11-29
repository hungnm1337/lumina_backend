using DataLayer.DTOs.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ServiceLayer.Role 
{ 
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    }
}