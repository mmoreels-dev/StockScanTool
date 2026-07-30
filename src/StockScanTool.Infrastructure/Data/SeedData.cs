using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Services;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context, IPasswordHasher passwordHasher, string adminPassword = "admin")
    {
        if (await context.Stores.AnyAsync())
            return;

        // ── Stores ────────────────────────────────────
        context.Stores.AddRange(
            new Store { Name = "Downtown Branch", Address = "123 Main St, City Center", IsActive = true },
            new Store { Name = "Mall Location", Address = "456 Commerce Ave, Shopping Mall", IsActive = true },
            new Store { Name = "Airport Terminal", Address = "789 Airport Rd, Terminal 2", IsActive = true },
            new Store { Name = "Suburban Plaza", Address = "321 Oak Lane, Suburbia", IsActive = true },
            new Store { Name = "Industrial Park", Address = "654 Factory Blvd, Industrial Zone", IsActive = true },
            new Store { Name = "Waterfront Store", Address = "987 Harbor Dr, Waterfront", IsActive = true }
        );

        // ── Permissions ───────────────────────────────
        var permissions = new List<Permission>
        {
            new() { Code = "dashboard.read", Name = "View Dashboard", Description = "View the dashboard summary", GroupName = "Dashboard" },

            new() { Code = "stores.read", Name = "View Stores", Description = "View store list and details", GroupName = "Stores" },
            new() { Code = "stores.create", Name = "Create Stores", Description = "Create new stores", GroupName = "Stores" },
            new() { Code = "stores.update", Name = "Update Stores", Description = "Edit existing stores", GroupName = "Stores" },
            new() { Code = "stores.delete", Name = "Delete Stores", Description = "Delete stores", GroupName = "Stores" },

            new() { Code = "products.read", Name = "View Products", Description = "View product list and details", GroupName = "Products" },
            new() { Code = "products.create", Name = "Create Products", Description = "Create new products", GroupName = "Products" },
            new() { Code = "products.update", Name = "Update Products", Description = "Edit existing products", GroupName = "Products" },
            new() { Code = "products.delete", Name = "Delete Products", Description = "Delete products", GroupName = "Products" },

            new() { Code = "devices.read", Name = "View Devices", Description = "View device list and details", GroupName = "Devices" },
            new() { Code = "devices.create", Name = "Create Devices", Description = "Create new scanning devices", GroupName = "Devices" },
            new() { Code = "devices.update", Name = "Update Devices", Description = "Edit existing devices", GroupName = "Devices" },
            new() { Code = "devices.delete", Name = "Delete Devices", Description = "Delete devices", GroupName = "Devices" },

            new() { Code = "inventory.read", Name = "View Inventory", Description = "View inventory levels", GroupName = "Inventory" },
            new() { Code = "inventory.create", Name = "Add Inventory", Description = "Add inventory records", GroupName = "Inventory" },
            new() { Code = "inventory.update", Name = "Update Inventory", Description = "Update inventory quantities", GroupName = "Inventory" },

            new() { Code = "sales.read", Name = "View Sales", Description = "View sale transactions", GroupName = "Sales" },
            new() { Code = "sales.create", Name = "Submit Sales", Description = "Submit new sale transactions", GroupName = "Sales" },

            new() { Code = "users.read", Name = "View Users", Description = "View user list and details", GroupName = "Users" },
            new() { Code = "users.create", Name = "Create Users", Description = "Create new users", GroupName = "Users" },
            new() { Code = "users.update", Name = "Update Users", Description = "Edit existing users", GroupName = "Users" },
            new() { Code = "users.delete", Name = "Delete Users", Description = "Delete users", GroupName = "Users" },

            new() { Code = "roles.read", Name = "View Roles", Description = "View role list and details", GroupName = "Roles" },
            new() { Code = "roles.create", Name = "Create Roles", Description = "Create new roles", GroupName = "Roles" },
            new() { Code = "roles.update", Name = "Update Roles", Description = "Edit existing roles", GroupName = "Roles" },
            new() { Code = "roles.delete", Name = "Delete Roles", Description = "Delete roles", GroupName = "Roles" },

            new() { Code = "scanning.sell", Name = "Sell via Scanner", Description = "Submit sales through scanner app", GroupName = "Scanning" },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();

        var permByCode = permissions.ToDictionary(p => p.Code);

        // ── Roles ─────────────────────────────────────
        var adminRole = new Role
        {
            Name = "Admin",
            Description = "Full system access",
            RolePermissions = permissions.Select(p => new RolePermission { PermissionId = p.Id }).ToList()
        };

        var managerRole = new Role
        {
            Name = "Manager",
            Description = "Operational access without user/role management",
            RolePermissions = permissions
                .Where(p => p.GroupName is "Dashboard" or "Stores" or "Products" or "Devices" or "Inventory" or "Sales")
                .Select(p => new RolePermission { PermissionId = p.Id }).ToList()
        };

        var viewerRole = new Role
        {
            Name = "Viewer",
            Description = "Read-only access",
            RolePermissions = permissions
                .Where(p => p.Code.EndsWith(".read"))
                .Select(p => new RolePermission { PermissionId = p.Id }).ToList()
        };

        context.Roles.AddRange(adminRole, managerRole, viewerRole);
        await context.SaveChangesAsync();

        // ── Admin User ────────────────────────────────
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = passwordHasher.Hash(adminPassword),
            DisplayName = "System Administrator",
            IsActive = true,
            MustChangePassword = false,
            UserRoles = [new UserRole { RoleId = adminRole.Id }]
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}
