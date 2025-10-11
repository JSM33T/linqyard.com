# Tier System Implementation Guide

This document explains how to implement and check user tiers in both backend (.NET) and frontend (Next.js/React).

## Overview

The Linqyard application has a 3-tier system:
- **Free Tier** (ID: 1) - Limited to 12 links and 2 groups
- **Plus Tier** (ID: 2) - Unlimited links and groups
- **Pro Tier** (ID: 3) - Unlimited links and groups + additional features

## Backend Implementation (.NET/EF Core)

### 1. Tier Entity

Located in: `Linqyard.Entities/Tier.cs`

```csharp
public class Tier
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

### 2. Tier Enum

Located in: `Linqyard.Entities/Enums/TierType.cs`

```csharp
namespace Linqyard.Entities.Enums
{
    public enum TierType
    {
        Free = 1,
        Plus = 2,
        Pro = 3
    }
}
```

**Why use enum?**
- Type safety
- Readable code (use `TierType.Free` instead of magic number `1`)
- IntelliSense support
- Easy to maintain

### 3. User Entity Update

Located in: `Linqyard.Entities/User.cs`

```csharp
public class User
{
    // ... existing properties ...
    
    public int? TierId { get; set; }
    
    [ForeignKey(nameof(TierId))]
    public Tier? Tier { get; set; }
}
```

### 4. Database Context Configuration

Located in: `Linqyard.Api/Data/LinqyardDbContext.cs`

```csharp
// Add DbSet
public DbSet<Tier> Tiers { get; set; }

// Configure entity
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...
    
    ConfigureTierEntity(modelBuilder);
    SeedTiers(modelBuilder);
}

private void ConfigureTierEntity(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Tier>(entity =>
    {
        entity.ToTable("Tiers");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Name).IsRequired().HasMaxLength(50);
        entity.Property(t => t.Description).HasMaxLength(500);
    });
}

private void SeedTiers(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Tier>().HasData(
        new Tier { Id = 1, Name = "free", Description = "Free tier - 12 links, 2 groups" },
        new Tier { Id = 2, Name = "plus", Description = "Plus tier - Unlimited links and groups" },
        new Tier { Id = 3, Name = "pro", Description = "Pro tier - All features unlocked" }
    );
}
```

### 5. Assign Default Tier on User Registration

Located in: `Linqyard.Api/Controllers/AuthController.cs`

```csharp
// Email/Password Registration
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    // ... validation code ...
    
    var user = new User
    {
        Email = emailLower,
        Username = request.Username,
        FirstName = request.FirstName,
        LastName = request.LastName,
        PasswordHash = hashedPassword,
        EmailVerified = false,
        TierId = (int)TierType.Free,  //  Assign free tier
        CreatedAt = DateTime.UtcNow
    };
    
    // ... save user ...
}

// Google OAuth Registration
[HttpGet("google/callback")]
public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
{
    // ... OAuth validation ...
    
    var newUser = new User
    {
        Email = emailLower,
        Username = username,
        FirstName = googleUser.GivenName ?? "User",
        LastName = googleUser.FamilyName ?? "",
        EmailVerified = googleUser.EmailVerified,
        TierId = (int)TierType.Free,  //  Assign free tier
        CreatedAt = DateTime.UtcNow
    };
    
    // ... save user ...
}
```

### 6. Include Tier in API Responses

Located in: `Linqyard.Api/Controllers/AuthController.cs`

```csharp
// Always include tier in queries
var user = await _context.Users
    .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
    .Include(u => u.Tier)  //  Include tier
    .FirstOrDefaultAsync(u => u.Email == emailLower);

// Return tier in response
return Ok(new ApiResponse<AuthResponse>
{
    Data = new AuthResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshTokenString,
        ExpiresAt = expiresAt,
        User = new UserInfo
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            Username = user.Username,
            // ... other fields ...
            TierId = user.TierId,           //  Include tier ID
            TierName = user.Tier?.Name,     //  Include tier name
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        }
    }
});
```

### 7. Tier-Based Resource Limits

Located in: `Linqyard.Api/Controllers/LinksController.cs` and `GroupsController.cs`

```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request)
{
    // Get current user with tier
    var user = await _context.Users
        .Include(u => u.Tier)
        .FirstOrDefaultAsync(u => u.Id == userId);
    
    //  Check free tier limits
    if (user?.TierId == (int)TierType.Free)
    {
        var linkCount = await _context.Links
            .CountAsync(l => l.UserId == userId && l.IsActive);
        
        if (linkCount >= 12)
        {
            return BadRequest(ProblemDetailsHelper.CreateValidationProblem(
                "Free tier limit reached",
                "You have reached the maximum of 12 links. Upgrade to Plus or Pro for unlimited links.",
                HttpContext
            ));
        }
    }
    
    // ... create link ...
}

[HttpPost("group/create")]
public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
{
    // Get current user with tier
    var user = await _context.Users
        .Include(u => u.Tier)
        .FirstOrDefaultAsync(u => u.Id == userId);
    
    //  Check free tier limits
    if (user?.TierId == (int)TierType.Free)
    {
        var groupCount = await _context.LinkGroups
            .CountAsync(g => g.UserId == userId && g.IsActive);
        
        if (groupCount >= 2)
        {
            return BadRequest(ProblemDetailsHelper.CreateValidationProblem(
                "Free tier limit reached",
                "You have reached the maximum of 2 groups. Upgrade to Plus or Pro for unlimited groups.",
                HttpContext
            ));
        }
    }
    
    // ... create group ...
}
```

### 8. Create Database Migration

```bash
# Navigate to API project
cd backend_dotnet/Linqyard.Api

# Create migration
dotnet ef migrations add AddTierSystem

# Apply migration
dotnet ef database update
```

## Frontend Implementation (Next.js/TypeScript)

### 1. Update User Interface

Located in: `frontend_nextjs/contexts/UserContext.tsx`

```typescript
export interface User {
  id?: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  avatarUrl?: string;
  coverUrl?: string;
  bio?: string;
  login: boolean;
  expiry?: Date;
  //  Tier information
  tierId?: number;
  tierName?: string;
  // Additional optional fields
  role?: string;
  preferences?: Record<string, any>;
}
```

### 2. Create Tier Helper Functions

Located in: `frontend_nextjs/contexts/UserContext.tsx`

```typescript
export const userHelpers = {
  // Get tier name with fallback
  getTierName: (user: User | null): string => {
    if (!user?.tierName) return 'free';
    return user.tierName;
  },

  //  Check if user is on free tier (defaults to true if no tier info)
  isFreeTier: (user: User | null): boolean => {
    // Default to free tier if tier info is not available
    if (!user) return true;
    if (user.tierId === undefined && user.tierName === undefined) return true;
    return user.tierId === 1 || user.tierName === 'free';
  },

  // Check if user is on plus tier
  isPlusTier: (user: User | null): boolean => {
    return user?.tierId === 2 || user?.tierName === 'plus';
  },

  // Check if user is on pro tier
  isProTier: (user: User | null): boolean => {
    return user?.tierId === 3 || user?.tierName === 'pro';
  },

  // Get tier display name (capitalized)
  getTierDisplayName: (user: User | null): string => {
    const tierName = userHelpers.getTierName(user);
    return tierName.charAt(0).toUpperCase() + tierName.slice(1);
  }
};
```

**Why default to free tier?**
- If tier data fails to load, we want to enforce limits (fail-safe)
- Prevents users from bypassing restrictions due to missing data
- Better user experience than showing errors

### 3. Update API Response Types

Located in: `frontend_nextjs/hooks/types.ts`

```typescript
export interface LoginResponse {
  data: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: {
      id: string;
      email: string;
      emailVerified: boolean;
      username: string;
      firstName: string;
      lastName: string;
      avatarUrl: string | null;
      coverUrl: string | null;
      createdAt: string;
      roles: string[];
      tierId?: number | null;      //  Add tier ID
      tierName?: string | null;    //  Add tier name
    };
  };
  meta: any | null;
}

// Update SignupResponse and GoogleCallbackResponse similarly
```

### 4. Save Tier Data on Login

Located in: `frontend_nextjs/app/account/login/page.tsx`

```typescript
// Handle successful login
if (response.status === 200 && response.data) {
  const { data } = response.data;
  
  setTokens(data.accessToken, data.refreshToken);
  
  //  Include tier data in user context
  setUser({
    id: data.user.id,
    firstName: data.user.firstName,
    lastName: data.user.lastName,
    username: data.user.username,
    email: data.user.email,
    avatarUrl: data.user.avatarUrl || undefined,
    login: true,
    expiry: new Date(data.expiresAt),
    tierId: data.user.tierId ?? undefined,        //  Convert null to undefined
    tierName: data.user.tierName ?? undefined,    //  Convert null to undefined
    role: data.user.roles[0] || 'user'
  });
  
  router.push('/');
}
```

### 5. Implement Frontend Tier Limits

Located in: `frontend_nextjs/app/account/links/page.tsx`

```typescript
import { userHelpers } from "@/contexts/UserContext";

export default function LinksPage() {
  const { user, isAuthenticated } = useUser();
  const { data: groupedData } = useGet<GetGroupedLinksResponse>("/link");
  
  //  Calculate total links and groups
  const totalLinks = useMemo(() => {
    const groupedLinks = localGroups.reduce((sum, group) => sum + group.links.length, 0);
    return groupedLinks + localUngrouped.length;
  }, [localGroups, localUngrouped]);

  const totalGroups = useMemo(() => localGroups.length, [localGroups]);

  //  Check tier and calculate permissions
  const isFreeTier = userHelpers.isFreeTier(user);
  const canCreateLink = !isFreeTier || totalLinks < 12;
  const canCreateGroup = !isFreeTier || totalGroups < 2;
  
  //  Validate before creating link
  function startCreate(groupId: string | null = null) {
    if (!canCreateLink) {
      toast.error("Free tier limit reached: Maximum 12 links allowed", {
        description: "Upgrade to Plus or Pro for unlimited links"
      });
      return;
    }
    
    setIsCreating(true);
    // ... rest of create logic ...
  }
  
  //  Disable UI elements when limits reached
  return (
    <>
      <DropdownMenu>
        <DropdownMenuContent>
          <DropdownMenuItem 
            onClick={() => canCreateLink && startCreate(null)}
            disabled={!canCreateLink}
            className={!canCreateLink ? "opacity-50 cursor-not-allowed" : ""}
          >
            <Plus className="h-4 w-4 mr-2" />
            New Link
            {!canCreateLink && (
              <span className="ml-2 text-xs text-muted-foreground">
                (Limit: 12)
              </span>
            )}
          </DropdownMenuItem>
          
          <DropdownMenuItem 
            onClick={() => {
              if (!canCreateGroup) {
                toast.error("Free tier limit reached: Maximum 2 groups allowed", {
                  description: "Upgrade to Plus or Pro for unlimited groups"
                });
                return;
              }
              setIsCreatingGroup(true);
            }}
            disabled={!canCreateGroup}
            className={!canCreateGroup ? "opacity-50 cursor-not-allowed" : ""}
          >
            <FolderPlus className="h-4 w-4 mr-2" />
            New Group
            {!canCreateGroup && (
              <span className="ml-2 text-xs text-muted-foreground">
                (Limit: 2)
              </span>
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
```

## Best Practices

### Backend
1.  **Always use TierType enum** instead of magic numbers
2.  **Include tier in all user queries** using `.Include(u => u.Tier)`
3.  **Check limits before creating resources** (links, groups)
4.  **Return descriptive error messages** with upgrade suggestions
5.  **Assign default free tier** on user registration
6.  **Validate tier limits at the API level** (never trust frontend)

### Frontend
1.  **Use helper functions** for tier checks (keeps code DRY)
2.  **Default to free tier** if tier data is missing (fail-safe)
3.  **Calculate totals with useMemo** for performance
4.  **Disable UI elements** when limits are reached (visual feedback)
5.  **Show toast notifications** with clear error messages
6.  **Display limit information** next to disabled options
7.  **Handle null values properly** (convert to undefined for React)

## Testing Checklist

### Backend Tests
- [ ] Free tier user can create up to 12 links
- [ ] Free tier user cannot create 13th link
- [ ] Free tier user can create up to 2 groups
- [ ] Free tier user cannot create 3rd group
- [ ] Plus/Pro tier users have no limits
- [ ] New users are assigned free tier by default
- [ ] Tier information is included in all auth responses

### Frontend Tests
- [ ] Tier data is saved to UserContext on login
- [ ] Link/group counts are calculated correctly
- [ ] Dropdown options are disabled when limits reached
- [ ] Toast messages appear when trying to exceed limits
- [ ] Limit text is displayed next to disabled options
- [ ] Plus/Pro users see no disabled options
- [ ] Tier defaults to free if data is missing

## Future Enhancements

1. **Tier Upgrade Flow**
   - Payment integration
   - Upgrade confirmation
   - Immediate tier update

2. **Additional Tier Limits**
   - Custom domains (Pro only)
   - Analytics retention (30 days free, unlimited Pro)
   - Custom branding (Pro only)
   - Priority support (Plus/Pro)

3. **Tier Badges**
   - Display tier badge on profile
   - Show tier benefits on upgrade page
   - Tier comparison table

## Quick Reference

### Backend Tier Check Pattern
```csharp
// Get user with tier
var user = await _context.Users
    .Include(u => u.Tier)
    .FirstOrDefaultAsync(u => u.Id == userId);

// Check limit
if (user?.TierId == (int)TierType.Free)
{
    var count = await _context.Things.CountAsync(t => t.UserId == userId);
    if (count >= LIMIT)
    {
        return BadRequest(ProblemDetailsHelper.CreateValidationProblem(
            "Free tier limit reached",
            $"Maximum {LIMIT} items allowed. Upgrade for unlimited access.",
            HttpContext
        ));
    }
}
```

### Frontend Tier Check Pattern
```typescript
// Import helpers
import { userHelpers } from "@/contexts/UserContext";

// Calculate limits
const isFreeTier = userHelpers.isFreeTier(user);
const canCreate = !isFreeTier || count < LIMIT;

// Check before action
if (!canCreate) {
  toast.error(`Free tier limit reached: Maximum ${LIMIT} allowed`, {
    description: "Upgrade to Plus or Pro for unlimited access"
  });
  return;
}

// Disable UI
<Button 
  disabled={!canCreate}
  className={!canCreate ? "opacity-50 cursor-not-allowed" : ""}
>
  Create
  {!canCreate && <span>(Limit: {LIMIT})</span>}
</Button>
```

## Database Schema

```sql
-- Tiers table
CREATE TABLE "Tiers" (
    "Id" INTEGER PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "Description" VARCHAR(500)
);

-- Insert default tiers
INSERT INTO "Tiers" VALUES 
    (1, 'free', 'Free tier - 12 links, 2 groups'),
    (2, 'plus', 'Plus tier - Unlimited links and groups'),
    (3, 'pro', 'Pro tier - All features unlocked');

-- Update Users table
ALTER TABLE "Users" ADD COLUMN "TierId" INTEGER;
ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Tiers_TierId" 
    FOREIGN KEY ("TierId") REFERENCES "Tiers" ("Id");

-- Set all existing users to free tier
UPDATE "Users" SET "TierId" = 1 WHERE "TierId" IS NULL;
```

---

**Last Updated:** October 11, 2025  
**Version:** 1.0  
**Author:** Development Team
