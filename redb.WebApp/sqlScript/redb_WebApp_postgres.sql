CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetRoles" (
        "Id" varchar(450) NOT NULL,
        "Name" varchar(256),
        "NormalizedName" varchar(256),
        "ConcurrencyStamp" TEXT,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetUsers" (
        "Id" varchar(450) NOT NULL,
        "UserName" varchar(256),
        "NormalizedUserName" varchar(256),
        "Email" varchar(256),
        "NormalizedEmail" varchar(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" TEXT,
        "SecurityStamp" TEXT,
        "ConcurrencyStamp" TEXT,
        "PhoneNumber" TEXT,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" int NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetRoleClaims" (
        "Id" int NOT NULL,
        "RoleId" varchar(450) NOT NULL,
        "ClaimType" TEXT,
        "ClaimValue" TEXT,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetUserClaims" (
        "Id" int NOT NULL,
        "UserId" varchar(450) NOT NULL,
        "ClaimType" TEXT,
        "ClaimValue" TEXT,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetUserLogins" (
        "LoginProvider" varchar(128) NOT NULL,
        "ProviderKey" varchar(128) NOT NULL,
        "ProviderDisplayName" TEXT,
        "UserId" varchar(450) NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetUserRoles" (
        "UserId" varchar(450) NOT NULL,
        "RoleId" varchar(450) NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE TABLE "AspNetUserTokens" (
        "UserId" varchar(450) NOT NULL,
        "LoginProvider" varchar(128) NOT NULL,
        "Name" varchar(128) NOT NULL,
        "Value" TEXT,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '00000000000000_CreateIdentitySchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('00000000000000_CreateIdentitySchema', '8.0.1');
    END IF;
END $EF$;
COMMIT;

INSERT INTO public."AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount") VALUES ('25e89bd5-74aa-45c7-98d8-a43c1a53374d', 'relikt@bk.ru', 'RELIKT@BK.RU', 'relikt@bk.ru', 'RELIKT@BK.RU', true, 'AQAAAAIAAYagAAAAEOrwhdehPFGlRUGGqjYQ2N43vAfkJazMZfGs2ok/43CzSm1w72GLClNEwPNQ1Zivew==', 'VTNSTYQB4RXJGJLMEMXACTKPLBBKGTGQ', 'bdc6f6d1-1308-4564-ba57-82e42884fc97', NULL, false, false, NULL, true, 0);
INSERT INTO public."AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount") VALUES ('040b1de7-911e-4fd3-8a58-3e0dbb76e122', 'sys', 'sys', 'sys@email.com', 'sys@EMAIL.COM', true, 'AQAAAAIAAYagAAAAEODF2RGdwhEtw/t2OKv37LOU9pEq2mKp8eaCFm4MEWA8BH+BXecu76LURuPP3fdK5w==', 'GUTY7TCX3ATOSGHLOS2AHS675BHINIR4', '6fbcb79f-7a03-44bc-b442-e19edb2b715f', NULL, false, false, NULL, true, 0);
INSERT INTO public." __EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('00000000000000_CreateIdentitySchema', '8.0.1');
