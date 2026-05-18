# ========================================================
# 0. VARIABLES DEFINITION (Sensitive Infrastructure Keys)
# ========================================================
variable "sql_admin_password" {
  type        = string
  description = "The administrator password for the SQL Server instance"
  sensitive   = true
}

variable "resend_api_key" {
  type        = string
  description = "The API key for Resend transactional email services"
  sensitive   = true
}

variable "jwt_key" {
  type        = string
  description = "The secret key for JWT signing (must be 32+ characters)"
  sensitive   = true
}

variable "resend_from_email" {
  type        = string
  description = "The verified sender email address on Resend"
  default     = "noreply@present.zainiqbal.tech"
}

# ========================================================
# 1. CORE AZURE PROVIDER CONFIGURATION
# ========================================================
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# ========================================================
# 2. THE RESOURCE GROUP (The Logical Perimeter)
# ========================================================
resource "azurerm_resource_group" "rg" {
  name     = "present-secops-rg"
  location = "southeastasia"
}

# ========================================================
# 3. AZURE CONTAINER REGISTRY (Our Private Docker Vault)
# ========================================================
resource "azurerm_container_registry" "acr" {
  name                = "presentregistry7007" # Must be globally unique
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic" # Absolute lowest cost private registry
  admin_enabled       = true    # Enables secure credentials for GitHub Actions
}

# ========================================================
# 4. APP SERVICE PLAN (The Virtual Hardware Allocator)
# ========================================================
resource "azurerm_service_plan" "plan" {
  name                = "present-linux-plan"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "B1" # Required tier to mount custom Linux Docker containers
}

# ========================================================
# 5. LINUX WEB APP FOR CONTAINERS (The App Runtime)
# ========================================================
resource "azurerm_linux_web_app" "app" {
  name                = "present-attendance-app"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.plan.id

  site_config {
    always_on = false # Keeps the app from eating compute credits when idle
    application_stack {
      docker_image_name        = "present-app:latest" # Combined name and tag to fit the modern spec
      docker_registry_url      = "https://${azurerm_container_registry.acr.login_server}"
      docker_registry_username = azurerm_container_registry.acr.admin_username
      docker_registry_password = azurerm_container_registry.acr.admin_password
    }
  }

  # Encrypted App Settings (Azure injects these as Environment Variables automatically)
  app_settings = {
    "WEBSITES_PORT"                                     = "8080" # Tells Azure which port Kestrel exposed in the Dockerfile
    "ConnectionStrings__ApplicationDbContextConnection" = "Server=tcp:${azurerm_mssql_server.sql.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};User ID=${azurerm_mssql_server.sql.administrator_login};Password=${var.sql_admin_password};"
    "Resend__ApiKey"                                    = var.resend_api_key
    "Resend__FromEmail"                                 = var.resend_from_email
    "Jwt__Key"                                          = var.jwt_key
    "Jwt__Issuer"                                       = "PresentAttendanceSystem"
    "Jwt__Audience"                                     = "PresentAttendanceSystemUsers"
  }
}

# ========================================================
# 6. AZURE SQL DATABASE SERVER (Cloud Data Engine)
# ========================================================
resource "azurerm_mssql_server" "sql" {
  name                         = "present-sqlserver-7007" # Must be globally unique
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = "secopsadmin"
  administrator_login_password = var.sql_admin_password
}

# The Isolated Database Instance
resource "azurerm_mssql_database" "db" {
  name           = "PresentProdDb"
  server_id      = azurerm_mssql_server.sql.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic" # Basic Tier, 5 DTUs (Approx $5/month—perfect for portfolio scale)
  zone_redundant = false
  storage_account_type = "Local" # Forces backups to stay in Singapore, bypassing the Geo policy block
}

# SecOps Firewall Rule: Allows internal Azure services (like our Web App) to talk to the SQL server
resource "azurerm_mssql_firewall_rule" "azure_internal" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}