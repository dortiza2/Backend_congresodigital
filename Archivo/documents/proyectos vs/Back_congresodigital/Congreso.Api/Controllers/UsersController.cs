using Congreso.Api.Data;
using Congreso.Api.Models;
using Congreso.Api.Services;
using Congreso.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Congreso.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IStaffService _staffService;

        public UsersController(IUserService userService, IStaffService staffService)
        {
            _userService = userService;
            _staffService = staffService;
        }

        [HttpGet]
        [Authorize(Policy = "RequireStaffOrHigher")]
        public async Task<ActionResult<ApiResponse<PagedResponseDto<UserDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var users = await _userService.GetUsersAsync(page, pageSize);
                return Ok(new ApiResponse<PagedResponseDto<UserDto>>
                {
                    Success = true,
                    Data = users,
                    Message = "Users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PagedResponseDto<UserDto>>
                {
                    Success = false,
                    Message = "error_retrieving_users"
                });
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = await _staffService.IsAdminAsync(currentUserId);

                // Users can only see their own profile unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return StatusCode(403, new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "insufficient_permissions" 
                    });
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return isAdmin ? NotFound(new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "user_not_found" 
                    }) : StatusCode(403, new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "insufficient_permissions" 
                    });
                }

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = user,
                    Message = "User retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserDto> 
                { 
                    Success = false,
                    Message = "error_retrieving_user"
                });
            }
        }

        [HttpGet("search")]
        [Authorize(Policy = "RequireStaffOrHigher")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> SearchUsers([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return Ok(new ApiResponse<List<UserDto>>
                    {
                        Success = true,
                        Data = new List<UserDto>(),
                        Message = "No search query provided"
                    });
                }

                var users = await _userService.SearchUsersAsync(q);
                return Ok(new ApiResponse<List<UserDto>>
                {
                    Success = true,
                    Data = users,
                    Message = "Users found successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<UserDto>>
                {
                    Success = false,
                    Message = "error_searching_users"
                });
            }
        }

        [HttpGet("role/{role}")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByRole(string role)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!await _staffService.IsAdminAsync(currentUserId))
                {
                    return StatusCode(403, new { message = "insufficient_permissions" });
                }

                var users = await _userService.GetUsersByRoleAsync(role);
                return Ok(users);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = "insufficient_permissions" });
            }
            catch (Exception ex)
            {
                return Ok(new OperationResponseDto { Success = false, Message = $"Error retrieving users by role: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate email uniqueness
                if (await _userService.EmailExistsAsync(dto.Email))
                {
                    return Ok(new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "email_already_exists" 
                    });
                }

                var user = await _userService.CreateUserAsync(dto);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = user,
                    Message = "User created successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new ApiResponse<UserDto> 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "error_creating_user"
                });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = await _staffService.IsAdminAsync(currentUserId);

                // Users can only update their own profile unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return StatusCode(403, new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "insufficient_permissions" 
                    });
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return isAdmin ? NotFound(new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "user_not_found" 
                    }) : StatusCode(403, new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "insufficient_permissions" 
                    });
                }

                var updatedUser = await _userService.UpdateUserAsync(id, dto);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = updatedUser,
                    Message = "User updated successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new ApiResponse<UserDto> 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "error_updating_user"
                });
            }
        }

        [HttpPut("{id:guid}/roles")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserRoles(Guid id, [FromBody] string[] roles)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto> 
                    { 
                        Success = false, 
                        Message = "user_not_found" 
                    });
                }

                var updatedUser = await _userService.UpdateUserRolesAsync(id, roles);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = updatedUser,
                    Message = "User roles updated successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new ApiResponse<UserDto> 
                { 
                    Success = false, 
                    Message = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "error_updating_user_roles"
                });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<bool> 
                    { 
                        Success = false, 
                        Message = "user_not_found" 
                    });
                }

                var success = await _userService.DeleteUserAsync(id);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = success,
                    Message = success ? "User deleted successfully" : "Failed to delete user"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "error_deleting_user"
                });
            }
        }

        [HttpGet("{id:guid}/enrollments")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<UserEnrollmentView>>>> GetUserEnrollments(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = await _staffService.IsAdminAsync(currentUserId);

                // Users can only see their own enrollments unless they're admin
                if (!isAdmin && currentUserId != id)
                {
                    return StatusCode(403, new ApiResponse<List<UserEnrollmentView>> 
                    { 
                        Success = false, 
                        Message = "insufficient_permissions" 
                    });
                }

                var enrollments = await _userService.GetUserEnrollmentsAsync(id);
                return Ok(new ApiResponse<List<UserEnrollmentView>>
                {
                    Success = true,
                    Data = enrollments ?? new List<UserEnrollmentView>(),
                    Message = "User enrollments retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<UserEnrollmentView>>
                {
                    Success = false,
                    Message = "error_retrieving_user_enrollments"
                });
            }
        }

        [HttpGet("exists/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUserExists(string email)
        {
            try
            {
                var exists = await _userService.EmailExistsAsync(email);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = "User existence check completed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "error_checking_user_exists"
                });
            }
        }

        #region Private Methods

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("user_not_authenticated");
            }
            return userId;
        }

        #endregion
    }
}