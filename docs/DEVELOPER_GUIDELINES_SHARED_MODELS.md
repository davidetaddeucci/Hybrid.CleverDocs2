# Developer Guidelines: Shared Models Management

## Quick Reference

### ✅ DO
- Use backend DTOs as the authoritative source
- Copy complete DTO definitions to frontend when needed
- Update both frontend and backend when changing shared DTOs
- Test compilation after DTO changes
- Document DTO changes in commit messages

### ❌ DON'T
- Create partial or incomplete DTO copies
- Modify DTO properties without updating both projects
- Create new DTOs without checking for existing ones
- Skip testing after DTO modifications

## Current Architecture

### Backend DTOs (Authoritative)
```
Hybrid.CleverDocs2.WebServices/
├── Models/Documents/
│   ├── UploadModels.cs          # Upload-related DTOs
│   ├── DocumentDto.cs           # Document management DTOs
│   └── ProcessingModels.cs      # R2R processing DTOs
└── Controllers/
    └── DocumentUploadController.cs  # API endpoints using DTOs
```

### Frontend Shared Models
```
Hybrid.CleverDocs.WebUI/
├── Models/Shared/
│   └── UploadModels.cs          # Copied from backend
└── Services/Documents/
    └── DocumentApiClient.cs     # Uses shared models
```

## Development Workflows

### Adding New DTOs

#### Backend First (Recommended)
1. Create DTO in backend project
2. Implement API endpoint using DTO
3. Test backend functionality
4. Copy DTO to frontend shared models
5. Update frontend services to use DTO
6. Test end-to-end functionality

#### Frontend First (When UI-driven)
1. Create DTO in frontend shared models
2. Implement frontend functionality
3. Copy DTO to backend project
4. Implement backend API endpoint
5. Test integration

### Modifying Existing DTOs

#### Process
1. **Identify Impact**: Check which projects use the DTO
2. **Backend Update**: Modify backend DTO first
3. **Frontend Sync**: Update frontend shared model
4. **Service Updates**: Update any services using the DTO
5. **Testing**: Compile and test both projects
6. **Documentation**: Update API documentation if needed

#### Example: Adding Property to UploadOptionsDto
```csharp
// 1. Backend: Hybrid.CleverDocs2.WebServices/Models/Documents/UploadModels.cs
public class UploadOptionsDto
{
    // ... existing properties ...
    public bool EnableCompression { get; set; } = false; // NEW PROPERTY
}

// 2. Frontend: Hybrid.CleverDocs.WebUI/Models/Shared/UploadModels.cs
public class UploadOptionsDto
{
    // ... existing properties ...
    public bool EnableCompression { get; set; } = false; // COPY NEW PROPERTY
}

// 3. Update services that use this DTO
// 4. Test compilation and functionality
```

### Removing DTOs

#### Process
1. **Dependency Check**: Find all usages of the DTO
2. **Replacement Plan**: Identify replacement DTOs or refactoring needed
3. **Backend Removal**: Remove from backend first
4. **Frontend Cleanup**: Remove from frontend shared models
5. **Service Updates**: Update all dependent services
6. **Testing**: Ensure no compilation errors

## Common Scenarios

### Scenario 1: API Contract Changes

**Situation**: Backend API changes require DTO modifications

**Steps**:
1. Update backend DTO to match new API contract
2. Update backend controller/service logic
3. Copy updated DTO to frontend shared models
4. Update frontend API client to handle changes
5. Test API integration

### Scenario 2: Frontend UI Requirements

**Situation**: Frontend needs additional properties for UI display

**Options**:
- **Option A**: Add to shared DTO if backend can support it
- **Option B**: Create separate ViewModel for UI-specific properties
- **Option C**: Use DTO extension methods for computed properties

**Recommended**: Option B for UI-specific data, Option A for data that backend should track

### Scenario 3: Version Compatibility

**Situation**: Need to support multiple API versions

**Strategy**:
- Use versioned DTOs (e.g., `UploadOptionsDtoV1`, `UploadOptionsDtoV2`)
- Maintain backward compatibility in shared models
- Use mapping between versions when needed

## Validation Checklist

### Before Committing DTO Changes

- [ ] Backend project compiles successfully
- [ ] Frontend project compiles successfully
- [ ] All unit tests pass
- [ ] Integration tests pass (if applicable)
- [ ] API documentation updated (if public API)
- [ ] Shared models documentation updated

### Code Review Checklist

- [ ] DTO changes are consistent between projects
- [ ] No partial or incomplete DTO copies
- [ ] Proper naming conventions followed
- [ ] Breaking changes are documented
- [ ] Migration path provided for breaking changes

## Troubleshooting

### Common Issues

#### Issue: Serialization Errors
**Symptoms**: JSON serialization/deserialization failures
**Cause**: Property name or type mismatches between frontend and backend
**Solution**: Verify DTO properties match exactly

#### Issue: Missing Properties
**Symptoms**: Properties are null or default values
**Cause**: Frontend DTO missing properties that backend expects
**Solution**: Copy complete DTO definition from backend

#### Issue: Compilation Errors
**Symptoms**: Build failures after DTO changes
**Cause**: References to old DTO properties or missing using statements
**Solution**: Update all references and using statements

### Debugging Steps

1. **Compare DTOs**: Use diff tools to compare frontend and backend DTOs
2. **Check Logs**: Look for serialization errors in application logs
3. **Test Endpoints**: Use tools like Postman to test API endpoints directly
4. **Verify JSON**: Compare JSON payloads between frontend and backend

## Tools and Utilities

### Recommended Tools

#### Code Comparison
- **Visual Studio**: Built-in file comparison
- **Beyond Compare**: Advanced diff tool
- **Git**: `git diff` for version comparison

#### API Testing
- **Postman**: API endpoint testing
- **Swagger UI**: Interactive API documentation
- **curl**: Command-line API testing

#### Code Analysis
- **SonarQube**: Code quality analysis
- **ReSharper**: Code inspection and refactoring
- **EditorConfig**: Consistent code formatting

### Automation Opportunities

#### Build Scripts
```bash
# Example: Validate DTO consistency
dotnet build Hybrid.CleverDocs2.WebServices
dotnet build Hybrid.CleverDocs.WebUI
# Run integration tests
dotnet test
```

#### Pre-commit Hooks
- Validate compilation
- Run DTO consistency checks
- Format code consistently

## Best Practices Summary

### Design Principles
1. **Single Source of Truth**: Backend DTOs are authoritative
2. **Fail Fast**: Compilation errors are better than runtime errors
3. **Explicit is Better**: Clear DTO definitions over implicit assumptions
4. **Test Early**: Validate changes as soon as possible

### Maintenance Principles
1. **Regular Audits**: Check for DTO drift periodically
2. **Documentation**: Keep guidelines and strategy documents updated
3. **Team Communication**: Discuss DTO changes in team meetings
4. **Continuous Improvement**: Refine processes based on experience

---

*Document Version: 1.0*  
*Last Updated: 2025-01-17*  
*Author: Augment Agent*
