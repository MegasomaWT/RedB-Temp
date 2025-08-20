using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using redb.Core.DBModels;
using redb.Core.Models.Contracts;
using redb.Core.Models.Entities;
using redb.Core.Models.Roles;
using redb.Core.Providers;

namespace redb.Core.Postgres.Providers
{
    /// <summary>
    /// PostgreSQL реализация провайдера ролей
    /// </summary>
    public class PostgresRoleProvider : IRoleProvider
    {
        private readonly RedbContext _context;
        private readonly IRedbSecurityContext _securityContext;

        public PostgresRoleProvider(RedbContext context, IRedbSecurityContext securityContext)
        {
            _context = context;
            _securityContext = securityContext;
        }

        // === CRUD РОЛЕЙ ===

        public async Task<IRedbRole> CreateRoleAsync(CreateRoleRequest request, IRedbUser? currentUser = null)
        {
            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Имя роли обязательно для заполнения");
            }

            if (request.Name.Length < 2)
            {
                throw new ArgumentException("Имя роли должно содержать минимум 2 символа");
            }

            // Проверка уникальности имени роли
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name);
            if (existingRole != null)
            {
                throw new InvalidOperationException($"Роль с именем '{request.Name}' уже существует");
            }

            // Создание новой роли
            var newRole = new _RRole
            {
                Id = _context.GetNextKey(),
                Name = request.Name
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            // Назначение пользователей в роль (если указаны)
            if (request.UserIds != null && request.UserIds.Length > 0)
            {
                foreach (var userId in request.UserIds)
                {
                    // Проверяем существование пользователя
                    var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                    if (userExists)
                    {
                        _context.Set<_RUsersRole>().Add(new _RUsersRole
                        {
                            IdUser = userId,
                            IdRole = newRole.Id
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedbRole.FromEntity(newRole);
        }

        public async Task<IRedbRole> UpdateRoleAsync(IRedbRole role, string newName, IRedbUser? currentUser = null)
        {
            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Новое имя роли обязательно для заполнения");
            }

            if (newName.Length < 2)
            {
                throw new ArgumentException("Имя роли должно содержать минимум 2 символа");
            }

            var dbRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
            if (dbRole == null)
            {
                throw new ArgumentException($"Роль с ID {role.Id} не найдена");
            }

            // Проверка уникальности нового имени (если оно отличается)
            if (dbRole.Name != newName)
            {
                var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == newName && r.Id != role.Id);
                if (existingRole != null)
                {
                    throw new InvalidOperationException($"Роль с именем '{newName}' уже существует");
                }

                dbRole.Name = newName;
                await _context.SaveChangesAsync();
            }

            return RedbRole.FromEntity(dbRole);
        }

        public async Task<bool> DeleteRoleAsync(IRedbRole role, IRedbUser? currentUser = null)
        {
            var dbRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
            if (dbRole == null)
            {
                return false; // Роль уже не существует
            }

            // Каскадное удаление связей пользователь-роль
            var userRoles = await _context.Set<_RUsersRole>()
                .Where(ur => ur.IdRole == role.Id)
                .ToListAsync();
            _context.Set<_RUsersRole>().RemoveRange(userRoles);

            // Каскадное удаление разрешений роли
            var rolePermissions = await _context.Set<_RPermission>()
                .Where(p => p.IdRole == role.Id)
                .ToListAsync();
            _context.Set<_RPermission>().RemoveRange(rolePermissions);

            // Удаление самой роли
            _context.Roles.Remove(dbRole);

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        // === ПОИСК РОЛЕЙ ===

        public async Task<IRedbRole?> GetRoleByIdAsync(long roleId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            return role != null ? RedbRole.FromEntity(role) : null;
        }

        public async Task<IRedbRole?> GetRoleByNameAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            return role != null ? RedbRole.FromEntity(role) : null;
        }

        public async Task<IRedbRole> LoadRoleAsync(long roleId)
        {
            var role = await GetRoleByIdAsync(roleId);
            if (role == null)
                throw new ArgumentException($"Роль с ID {roleId} не найдена");
            return role;
        }

        public async Task<IRedbRole> LoadRoleAsync(string roleName)
        {
            var role = await GetRoleByNameAsync(roleName);
            if (role == null)
                throw new ArgumentException($"Роль с именем '{roleName}' не найдена");
            return role;
        }

        public async Task<List<IRedbRole>> GetRolesAsync()
        {
            var roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync();
            return roles.Select(r => RedbRole.FromEntity(r)).Cast<IRedbRole>().ToList();
        }

        // === УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЬ-РОЛЬ ===

        public async Task<bool> AssignUserToRoleAsync(IRedbUser user, IRedbRole role, IRedbUser? currentUser = null)
        {
            // Проверяем существование пользователя и роли
            var userExists = await _context.Users.AnyAsync(u => u.Id == user.Id);
            if (!userExists)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            var roleExists = await _context.Roles.AnyAsync(r => r.Id == role.Id);
            if (!roleExists)
            {
                throw new ArgumentException($"Роль с ID {role.Id} не найдена");
            }

            // Проверяем, не назначена ли уже эта роль пользователю
            var existingAssignment = await _context.UsersRoles
                .AnyAsync(ur => ur.IdUser == user.Id && ur.IdRole == role.Id);
            
            if (existingAssignment)
            {
                return true; // Роль уже назначена
            }

            // Создаем новую связь
            _context.UsersRoles.Add(new _RUsersRole
            {
                IdUser = user.Id,
                IdRole = role.Id
            });

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> RemoveUserFromRoleAsync(IRedbUser user, IRedbRole role, IRedbUser? currentUser = null)
        {
            var userRole = await _context.UsersRoles
                .FirstOrDefaultAsync(ur => ur.IdUser == user.Id && ur.IdRole == role.Id);
            
            if (userRole == null)
            {
                return false; // Связь не существует
            }

            _context.UsersRoles.Remove(userRole);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> SetUserRolesAsync(IRedbUser user, IRedbRole[] roles, IRedbUser? currentUser = null)
        {
            // Проверяем существование пользователя
            var userExists = await _context.Users.AnyAsync(u => u.Id == user.Id);
            if (!userExists)
            {
                throw new ArgumentException($"Пользователь с ID {user.Id} не найден");
            }

            // Удаляем все существующие роли пользователя
            var existingRoles = await _context.UsersRoles
                .Where(ur => ur.IdUser == user.Id)
                .ToListAsync();
            _context.UsersRoles.RemoveRange(existingRoles);

            // Добавляем новые роли
            if (roles != null && roles.Length > 0)
            {
                foreach (var role in roles)
                {
                    // Проверяем существование роли
                    var roleExists = await _context.Roles.AnyAsync(r => r.Id == role.Id);
                    if (roleExists)
                    {
                        _context.UsersRoles.Add(new _RUsersRole
                        {
                            IdUser = user.Id,
                            IdRole = role.Id
                        });
                    }
                }
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<List<IRedbRole>> GetUserRolesAsync(IRedbUser user)
        {
            var roles = await _context.UsersRoles
                .Where(ur => ur.IdUser == user.Id)
                .Include(ur => ur.RoleNavigation)
                .Select(ur => ur.RoleNavigation)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(r => RedbRole.FromEntity(r)).Cast<IRedbRole>().ToList();
        }

        public async Task<List<IRedbUser>> GetRoleUsersAsync(IRedbRole role)
        {
            var users = await _context.UsersRoles
                .Where(ur => ur.IdRole == role.Id)
                .Include(ur => ur.UserNavigation)
                .Select(ur => ur.UserNavigation)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return users.Select(u => RedbUser.FromEntity(u)).Cast<IRedbUser>().ToList();
        }

        public async Task<bool> UserHasRoleAsync(IRedbUser user, IRedbRole role)
        {
            return await _context.UsersRoles
                .AnyAsync(ur => ur.IdUser == user.Id && ur.IdRole == role.Id);
        }

        // === УСТАРЕВШИЕ МЕТОДЫ (будут закомментированы) ===
        
        /*
        public async Task<bool> AssignUserToRoleAsync(long userId, long roleId, IRedbUser? currentUser = null)
        {
            // Назначение роли пользователю
            throw new NotImplementedException("AssignUserToRoleAsync будет реализован в следующих итерациях");
        }

        public async Task<bool> AssignUserToRoleAsync(long userId, string roleName, IRedbUser? currentUser = null)
        {
            var role = await GetRoleByNameAsync(roleName);
            if (role == null)
                throw new ArgumentException($"Роль с именем '{roleName}' не найдена");
            
            return await AssignUserToRoleAsync(userId, role.Id, currentUser);
        }

        public async Task<bool> RemoveUserFromRoleAsync(long userId, long roleId, IRedbUser? currentUser = null)
        {
            // Удаление роли у пользователя
            throw new NotImplementedException("RemoveUserFromRoleAsync будет реализован в следующих итерациях");
        }

        public async Task<bool> RemoveUserFromRoleAsync(long userId, string roleName, IRedbUser? currentUser = null)
        {
            var role = await GetRoleByNameAsync(roleName);
            if (role == null)
                return false; // Роль не найдена, считаем что удаление успешно
            
            return await RemoveUserFromRoleAsync(userId, role.Id, currentUser);
        }

        public async Task<bool> SetUserRolesAsync(long userId, long[] roleIds, IRedbUser? currentUser = null)
        {
            // Установка ролей пользователя (заменить все существующие)
            throw new NotImplementedException("SetUserRolesAsync будет реализован в следующих итерациях");
        }

        public async Task<List<_RRole>> GetUserRolesAsync(long userId)
        {
            return await _context.UsersRoles
                .Where(ur => ur.IdUser == userId)
                .Include(ur => ur.RoleNavigation)
                .Select(ur => ur.RoleNavigation)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<List<IRedbUser>> GetRoleUsersAsync(long roleId)
        {
            var users = await _context.UsersRoles
                .Where(ur => ur.IdRole == roleId)
                .Include(ur => ur.UserNavigation)
                .Select(ur => ur.UserNavigation)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return users.Select(u => RedbUser.FromEntity(u)).Cast<IRedbUser>().ToList();
        }

        public async Task<bool> UserHasRoleAsync(long userId, long roleId)
        {
            return await _context.UsersRoles
                .AnyAsync(ur => ur.IdUser == userId && ur.IdRole == roleId);
        }

        public async Task<bool> UserHasRoleAsync(long userId, string roleName)
        {
            return await _context.UsersRoles
                .Include(ur => ur.RoleNavigation)
                .AnyAsync(ur => ur.IdUser == userId && ur.RoleNavigation.Name == roleName);
        }
        */

        // === ВАЛИДАЦИЯ ===

        public async Task<bool> IsRoleNameAvailableAsync(string roleName, IRedbRole? excludeRole = null)
        {
            var query = _context.Roles.Where(r => r.Name == roleName);
            if (excludeRole != null)
                query = query.Where(r => r.Id != excludeRole.Id);
            
            return !await query.AnyAsync();
        }

        // === СТАТИСТИКА ===

        public async Task<int> GetRoleCountAsync()
        {
            return await _context.Roles.CountAsync();
        }

        public async Task<int> GetRoleUserCountAsync(IRedbRole role)
        {
            return await _context.UsersRoles.CountAsync(ur => ur.IdRole == role.Id);
        }

        public async Task<Dictionary<IRedbRole, int>> GetRoleStatisticsAsync()
        {
            var roleStats = await _context.Roles
                .GroupJoin(_context.UsersRoles,
                    role => role.Id,
                    userRole => userRole.IdRole,
                    (role, userRoles) => new { Role = role, UserCount = userRoles.Count() })
                .ToListAsync();

            return roleStats.ToDictionary(
                x => (IRedbRole)RedbRole.FromEntity(x.Role), 
                x => x.UserCount);
        }

    }
}
