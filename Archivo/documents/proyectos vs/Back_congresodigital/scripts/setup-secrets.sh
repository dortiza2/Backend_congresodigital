#!/bin/bash

# GitHub Secrets Setup Script for Congreso Digital
# This script helps set up the required GitHub secrets for CI/CD deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if GitHub CLI is installed
check_github_cli() {
    if ! command -v gh &> /dev/null; then
        print_error "GitHub CLI (gh) is not installed. Please install it first:"
        print_error "https://cli.github.com/"
        exit 1
    fi
}

# Function to check if user is authenticated with GitHub CLI
check_github_auth() {
    if ! gh auth status &> /dev/null; then
        print_error "You are not authenticated with GitHub CLI. Please run:"
        print_error "gh auth login"
        exit 1
    fi
}

# Function to get repository information
get_repo_info() {
    REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "")
    if [ -z "$REPO" ]; then
        print_error "Could not determine repository. Make sure you're in a GitHub repository directory."
        exit 1
    fi
    print_success "Repository: $REPO"
}

# Function to prompt for secret value
prompt_for_secret() {
    local secret_name=$1
    local description=$2
    local required=${3:-true}
    
    echo ""
    print_status "Setting up: $secret_name"
    echo "Description: $description"
    
    if [ "$required" = false ]; then
        echo "This is optional - press Enter to skip"
    fi
    
    read -sp "Enter value for $secret_name: " secret_value
    echo ""
    
    if [ -z "$secret_value" ] && [ "$required" = true ]; then
        print_error "$secret_name is required. Please try again."
        prompt_for_secret "$secret_name" "$description" "$required"
    elif [ -z "$secret_value" ] && [ "$required" = false ]; then
        echo "SKIPPED"
        return 1
    fi
    
    echo "$secret_value"
    return 0
}

# Function to set GitHub secret
set_github_secret() {
    local secret_name=$1
    local secret_value=$2
    
    if [ -z "$secret_value" ]; then
        print_warning "Skipping $secret_name (empty value)"
        return 0
    fi
    
    echo -n "$secret_value" | gh secret set "$secret_name" -R "$REPO"
    if [ $? -eq 0 ]; then
        print_success "✓ Set $secret_name"
    else
        print_error "✗ Failed to set $secret_name"
        return 1
    fi
}

# Function to generate random password
generate_password() {
    openssl rand -base64 32 2>/dev/null || date | md5sum | head -c 32
}

# Main setup function
main() {
    echo ""
    echo "=========================================="
    echo "  Congreso Digital - GitHub Secrets Setup"
    echo "=========================================="
    echo ""
    
    # Check prerequisites
    check_github_cli
    check_github_auth
    get_repo_info
    
    echo ""
    print_status "This script will help you set up all required GitHub secrets for the CI/CD pipeline."
    print_warning "Make sure you have all your API keys, passwords, and configuration values ready."
    echo ""
    
    read -p "Do you want to continue? (y/N): " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Setup cancelled."
        exit 0
    fi
    
    echo ""
    echo "=========================================="
    echo "  DATABASE CONFIGURATION"
    echo "=========================================="
    
    # Database secrets
    DATABASE_CONNECTION_STRING=$(prompt_for_secret "DATABASE_CONNECTION_STRING" "PostgreSQL connection string (format: Host=hostname;Database=dbname;Username=user;Password=password;Port=5432)")
    set_github_secret "DATABASE_CONNECTION_STRING" "$DATABASE_CONNECTION_STRING"
    
    echo ""
    echo "=========================================="
    echo "  JWT AUTHENTICATION"
    echo "=========================================="
    
    # JWT secrets
    JWT_SECRET_KEY=$(prompt_for_secret "JWT_SECRET_KEY" "JWT secret key (minimum 32 characters, will be generated if empty)")
    if [ -z "$JWT_SECRET_KEY" ]; then
        JWT_SECRET_KEY=$(generate_password)
        print_success "Generated JWT secret key"
    fi
    set_github_secret "JWT_SECRET_KEY" "$JWT_SECRET_KEY"
    
    JWT_ISSUER=$(prompt_for_secret "JWT_ISSUER" "JWT issuer (e.g., congreso-digital-api)")
    set_github_secret "JWT_ISSUER" "$JWT_ISSUER"
    
    JWT_AUDIENCE=$(prompt_for_secret "JWT_AUDIENCE" "JWT audience (e.g., congreso-digital-app)")
    set_github_secret "JWT_AUDIENCE" "$JWT_AUDIENCE"
    
    echo ""
    echo "=========================================="
    echo "  EMAIL CONFIGURATION (SMTP)"
    echo "=========================================="
    
    # Email secrets
    SMTP_HOST=$(prompt_for_secret "SMTP_HOST" "SMTP server hostname")
    set_github_secret "SMTP_HOST" "$SMTP_HOST"
    
    SMTP_PORT=$(prompt_for_secret "SMTP_PORT" "SMTP server port (usually 587 or 25)")
    set_github_secret "SMTP_PORT" "$SMTP_PORT"
    
    SMTP_USERNAME=$(prompt_for_secret "SMTP_USERNAME" "SMTP username/email")
    set_github_secret "SMTP_USERNAME" "$SMTP_USERNAME"
    
    SMTP_PASSWORD=$(prompt_for_secret "SMTP_PASSWORD" "SMTP password")
    set_github_secret "SMTP_PASSWORD" "$SMTP_PASSWORD"
    
    EMAIL_FROM_ADDRESS=$(prompt_for_secret "EMAIL_FROM_ADDRESS" "Sender email address")
    set_github_secret "EMAIL_FROM_ADDRESS" "$EMAIL_FROM_ADDRESS"
    
    EMAIL_FROM_NAME=$(prompt_for_secret "EMAIL_FROM_NAME" "Sender name (e.g., Congreso Digital)")
    set_github_secret "EMAIL_FROM_NAME" "$EMAIL_FROM_NAME"
    
    echo ""
    echo "=========================================="
    echo "  PODIUM INTEGRATION"
    echo "=========================================="
    
    # Podium secrets
    PODIUM_API_URL=$(prompt_for_secret "PODIUM_API_URL" "Podium API URL")
    set_github_secret "PODIUM_API_URL" "$PODIUM_API_URL"
    
    PODIUM_API_KEY=$(prompt_for_secret "PODIUM_API_KEY" "Podium API key")
    set_github_secret "PODIUM_API_KEY" "$PODIUM_API_KEY"
    
    echo ""
    echo "=========================================="
    echo "  RENDER DEPLOYMENT"
    echo "=========================================="
    
    # Render secrets
    RENDER_API_KEY=$(prompt_for_secret "RENDER_API_KEY" "Render.com API key (get from https://dashboard.render.com/api-keys)")
    set_github_secret "RENDER_API_KEY" "$RENDER_API_KEY"
    
    print_status "RENDER_SERVICE_ID will be set automatically during deployment"
    
    echo ""
    echo "=========================================="
    echo "  FRONTEND CONFIGURATION"
    echo "=========================================="
    
    # Frontend URLs
    FRONTEND_URL=$(prompt_for_secret "FRONTEND_URL" "Production frontend URL (e.g., https://congreso-digital.com)")
    set_github_secret "FRONTEND_URL" "$FRONTEND_URL"
    
    FRONTEND_RESET_PASSWORD_URL=$(prompt_for_secret "FRONTEND_RESET_PASSWORD_URL" "Frontend password reset URL")
    set_github_secret "FRONTEND_RESET_PASSWORD_URL" "$FRONTEND_RESET_PASSWORD_URL"
    
    FRONTEND_VERIFY_EMAIL_URL=$(prompt_for_secret "FRONTEND_VERIFY_EMAIL_URL" "Frontend email verification URL")
    set_github_secret "FRONTEND_VERIFY_EMAIL_URL" "$FRONTEND_VERIFY_EMAIL_URL"
    
    echo ""
    echo "=========================================="
    echo "  OPTIONAL INTEGRATIONS"
    echo "=========================================="
    
    # Optional integrations
    print_status "Optional integrations - press Enter to skip any of these"
    
    STRIPE_SECRET_KEY=$(prompt_for_secret "STRIPE_SECRET_KEY" "Stripe secret key (optional)" false)
    if [ -n "$STRIPE_SECRET_KEY" ]; then
        set_github_secret "STRIPE_SECRET_KEY" "$STRIPE_SECRET_KEY"
    fi
    
    STRIPE_WEBHOOK_SECRET=$(prompt_for_secret "STRIPE_WEBHOOK_SECRET" "Stripe webhook secret (optional)" false)
    if [ -n "$STRIPE_WEBHOOK_SECRET" ]; then
        set_github_secret "STRIPE_WEBHOOK_SECRET" "$STRIPE_WEBHOOK_SECRET"
    fi
    
    SENDGRID_API_KEY=$(prompt_for_secret "SENDGRID_API_KEY" "SendGrid API key (optional)" false)
    if [ -n "$SENDGRID_API_KEY" ]; then
        set_github_secret "SENDGRID_API_KEY" "$SENDGRID_API_KEY"
    fi
    
    AZURE_STORAGE_CONNECTION_STRING=$(prompt_for_secret "AZURE_STORAGE_CONNECTION_STRING" "Azure Storage connection string (optional)" false)
    if [ -n "$AZURE_STORAGE_CONNECTION_STRING" ]; then
        set_github_secret "AZURE_STORAGE_CONNECTION_STRING" "$AZURE_STORAGE_CONNECTION_STRING"
    fi
    
    echo ""
    echo "=========================================="
    echo "  ADDITIONAL SECURITY SECRETS"
    echo "=========================================="
    
    # Additional security secrets
    EXTERNAL_API_KEY=$(prompt_for_secret "EXTERNAL_API_KEY" "External API key (optional)" false)
    if [ -n "$EXTERNAL_API_KEY" ]; then
        set_github_secret "EXTERNAL_API_KEY" "$EXTERNAL_API_KEY"
    fi
    
    WEBHOOK_SECRET=$(prompt_for_secret "WEBHOOK_SECRET" "Webhook secret (optional)" false)
    if [ -n "$WEBHOOK_SECRET" ]; then
        set_github_secret "WEBHOOK_SECRET" "$WEBHOOK_SECRET"
    fi
    
    # Generate additional secrets
    print_status "Generating additional security secrets..."
    
    CERTIFICATE_ENCRYPTION_KEY=$(generate_password)
    set_github_secret "CERTIFICATE_ENCRYPTION_KEY" "$CERTIFICATE_ENCRYPTION_KEY"
    print_success "Generated certificate encryption key"
    
    DATA_PROTECTION_KEY=$(generate_password)
    set_github_secret "DATA_PROTECTION_KEY" "$DATA_PROTECTION_KEY"
    print_success "Generated data protection key"
    
    echo ""
    echo "=========================================="
    echo "  SETUP COMPLETE"
    echo "=========================================="
    echo ""
    print_success "All GitHub secrets have been configured successfully!"
    echo ""
    print_status "Next steps:"
    echo "1. Review your GitHub repository secrets at:"
    echo "   https://github.com/$REPO/settings/secrets/actions"
    echo ""
    echo "2. Test the deployment pipeline by pushing to the main branch"
    echo ""
    echo "3. Monitor the deployment at:"
    echo "   https://github.com/$REPO/actions"
    echo ""
    print_warning "Important security notes:"
    echo "- Keep your secrets secure and never share them"
    echo "- Regularly rotate API keys and passwords"
    echo "- Monitor GitHub Actions logs for any security issues"
    echo "- Use different secrets for different environments (staging, production)"
    echo ""
    print_success "Setup complete! 🚀"
}

# Run the main function
main "$@"