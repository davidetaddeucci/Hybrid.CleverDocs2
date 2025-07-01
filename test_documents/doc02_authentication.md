# Authentication System

## Overview
The authentication system uses a hybrid approach combining cookie authentication for the WebUI and JWT tokens for API calls.

### Authentication Flow
1. User logs in via WebUI form
2. Server validates credentials against PostgreSQL
3. JWT token generated and stored in HttpOnly cookie
4. Subsequent API calls use JWT from cookie

### Security Features
- Password hashing with bcrypt
- JWT tokens with configurable expiration
- HttpOnly cookies prevent XSS attacks
- CSRF protection on forms
- Rate limiting on login attempts

### User Management
- Company-based multi-tenancy
- Role-based permissions (Admin, User, Viewer)
- Email verification for new accounts
- Password reset functionality

### R2R Integration
- Each user gets corresponding R2R user account
- Automatic sync of user data to R2R
- Tenant isolation maintained in R2R

The system ensures maximum security while maintaining usability for enterprise environments.