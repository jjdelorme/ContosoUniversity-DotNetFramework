# Contoso University - ASP.NET Core 9

This is the migrated version of Contoso University, upgraded from .NET Framework 4.5 to ASP.NET Core 9 for deployment on Google Cloud Run.

## Key Migration Changes

### Architecture Updates
- **Framework**: Migrated from .NET Framework 4.5 to .NET 9
- **ORM**: Upgraded from Entity Framework 6 to Entity Framework Core 9
- **Authentication**: Implemented ASP.NET Core Identity with role-based authorization
- **Dependency Injection**: Replaced direct instantiation with built-in DI container
- **Configuration**: Moved from Web.config to appsettings.json
- **Logging**: Integrated Serilog with Google Cloud Logging

### Security Enhancements
- Added authentication and authorization using ASP.NET Core Identity
- Implemented security headers (CSP, HSTS, X-Frame-Options, etc.)
- Updated all JavaScript libraries to latest secure versions (Bootstrap 5, jQuery removed)
- Added proper input validation and anti-forgery tokens
- Configured secure password policies

### Cloud-Ready Features
- Containerized with Docker for Linux deployment
- Configured for Google Cloud Run
- Integrated Google Cloud Secret Manager for sensitive configuration
- Added resilient SQL connection with retry policies
- Implemented structured logging with Google Cloud Logging

## Prerequisites

- .NET 9 SDK
- Docker Desktop
- Google Cloud SDK
- Google Cloud Project with enabled APIs:
  - Cloud Run API
  - Cloud SQL API
  - Secret Manager API
  - Cloud Build API

## Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ContosoUniversity/ContosoUniversity.Core
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database connection string**
   Edit `appsettings.Development.json` with your local SQL Server connection

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

## Google Cloud Deployment

### 1. Set up Google Cloud SQL

```bash
# Create Cloud SQL instance
gcloud sql instances create contoso-sql \
  --database-version=SQLSERVER_2019_STANDARD \
  --tier=db-n1-standard-2 \
  --region=us-central1

# Create database
gcloud sql databases create ContosoUniversity \
  --instance=contoso-sql

# Create user
gcloud sql users create sqlserver \
  --instance=contoso-sql \
  --password=<secure-password>
```

### 2. Set up Secret Manager

```bash
# Create secrets
echo -n "Server=127.0.0.1;Database=ContosoUniversity;User Id=sqlserver;Password=<password>;TrustServerCertificate=True" | \
  gcloud secrets create contoso-db-connection --data-file=-

echo -n "your-project-id" | \
  gcloud secrets create google-project-id --data-file=-
```

### 3. Create Service Account

```bash
# Create service account
gcloud iam service-accounts create contoso-university-sa \
  --display-name="Contoso University Service Account"

# Grant necessary permissions
gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="serviceAccount:contoso-university-sa@PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/cloudsql.client"

gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="serviceAccount:contoso-university-sa@PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

gcloud projects add-iam-policy-binding PROJECT_ID \
  --member="serviceAccount:contoso-university-sa@PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/logging.logWriter"
```

### 4. Deploy to Cloud Run

```bash
# Submit build
gcloud builds submit --config=cloudbuild.yaml .

# Or deploy directly
gcloud run deploy contoso-university \
  --source . \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --set-secrets ConnectionStrings__DefaultConnection=contoso-db-connection:latest \
  --set-secrets Google__ProjectId=google-project-id:latest \
  --service-account contoso-university-sa@PROJECT_ID.iam.gserviceaccount.com
```

### 5. Set up Cloud SQL Proxy (for production)

The application is configured to connect to Cloud SQL via the Cloud SQL proxy. Cloud Run automatically provides this at 127.0.0.1.

## Architecture

### Models
- **Person** (base class): Student, Instructor
- **Course**: Course information with many-to-many relationship with Instructors
- **Department**: Academic departments
- **Enrollment**: Student course enrollments
- **OfficeAssignment**: Instructor office locations
- **CourseAssignment**: Join table for Course-Instructor relationship

### Security Model
- **Roles**: Administrator, Instructor, Student
- **Default Admin**: admin@contoso.edu (password: Admin123!)
- **Authorization**: 
  - Students: View access
  - Instructors: Can manage courses
  - Administrators: Full access

### Performance Optimizations
- Async/await throughout controllers
- No-tracking queries for read operations
- Efficient pagination with X.PagedList
- Connection resiliency with retry policies
- Proper use of Include/ThenInclude for eager loading

## Monitoring

The application logs to Google Cloud Logging. Key metrics to monitor:
- Request latency
- Database query performance
- Error rates
- Authentication failures

## Troubleshooting

1. **Database Connection Issues**
   - Verify Cloud SQL proxy is running
   - Check Secret Manager permissions
   - Ensure connection string is correct

2. **Authentication Issues**
   - Check if Identity migrations have run
   - Verify cookie settings in production

3. **Performance Issues**
   - Review Cloud Logging for slow queries
   - Check Cloud Run scaling settings
   - Monitor database connection pool

## Contributing

Please ensure all changes maintain:
- Security best practices
- Async patterns
- Proper error handling
- Unit test coverage
- Documentation updates