using DataLayer.DTOs.User;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IUserRepository
{
    Task<(IEnumerable<UserDto> users, int totalPages)> GetFilteredUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, string? roleName);

    Task<User> GetUserByIdAsync(int userId); 
    Task<bool> ToggleUserStatusAsync(int userId);   
}


