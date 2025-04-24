
when data model is updated
```
dotnet ef migrations add Database_v0
dotnet ef migrations add Database_v1
```

for testing, creating DB

-- Step 1: Ensure the schema exists
CREATE SCHEMA IF NOT EXISTS "BTCPayServer.Plugins.Lqwd";

-- Step 2: Create the PluginRecords table
CREATE TABLE IF NOT EXISTS "BTCPayServer.Plugins.Lqwd"."PluginRecords" (
    "Id" TEXT PRIMARY KEY,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Step 3: Create the Orders table
CREATE TABLE IF NOT EXISTS "BTCPayServer.Plugins.Lqwd"."Orders" (
    "OrderId" TEXT PRIMARY KEY,
    "StoreId" TEXT NOT NULL,
    "LspBalanceSat" BIGINT NOT NULL,
    "ClientBalanceSat" BIGINT NOT NULL,
    "RequiredChannelConfirmations" INT NOT NULL,
    "FundingConfirmsWithinBlocks" INT NOT NULL,
    "ChannelExpiryBlocks" INT NOT NULL,
    "Token" TEXT NULL,
    "RefundOnchainAddress" TEXT NULL,
    "AnnounceChannel" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "OrderState" TEXT NOT NULL,
    "PaymentInvoice" TEXT NULL,
    "OnchainAddress" TEXT NULL
);
