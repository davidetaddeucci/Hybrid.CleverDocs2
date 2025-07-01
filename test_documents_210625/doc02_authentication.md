# Authentication System Documentation

## JWT Authentication
The system implements a hybrid authentication approach combining Cookie Authentication for WebUI with JWT tokens in HttpOnly cookies for API calls.

### Security Features
- Multi-tenant isolation
- Role-based access control (Admin, Company, User)
- Secure token storage in HttpOnly cookies
- Automatic token refresh
- Session management

### User Roles
1. **Admin (1)**: Full system access
2. **Company (2)**: Company management access  
3. **User (3)**: Standard user access

### Test Credentials
- Admin: info@hybrid.it / Florealia2025!
- Company: info@microsis.it / Maremmabona1!
- User: r.antoniucci@microsis.it / Maremmabona1!

This document tests authentication-related content processing.