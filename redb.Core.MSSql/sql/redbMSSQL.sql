USE [redb]
GO
/****** Object:  Trigger [TR__objects__deleted_objects]    Script Date: 29.04.2024 13:21:00 ******/
DROP TRIGGER [dbo].[TR__objects__deleted_objects]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetChildObjects]    Script Date: 29.04.2024 13:21:00 ******/
DROP PROCEDURE [dbo].[sp_GetChildObjects]
GO
ALTER TABLE [dbo].[_permissions] DROP CONSTRAINT [CK__permissions_users_roles]
GO
ALTER TABLE [dbo].[_links] DROP CONSTRAINT [CK__links]
GO
ALTER TABLE [dbo].[_values] DROP CONSTRAINT [FK__values__structures]
GO
ALTER TABLE [dbo].[_values] DROP CONSTRAINT [FK__values__objects]
GO
ALTER TABLE [dbo].[_users_roles] DROP CONSTRAINT [FK__users_roles__users]
GO
ALTER TABLE [dbo].[_users_roles] DROP CONSTRAINT [FK__users_roles__roles]
GO
ALTER TABLE [dbo].[_structures] DROP CONSTRAINT [FK__structures__types]
GO
ALTER TABLE [dbo].[_structures] DROP CONSTRAINT [FK__structures__structures]
GO
ALTER TABLE [dbo].[_structures] DROP CONSTRAINT [FK__structures__schemes]
GO
ALTER TABLE [dbo].[_structures] DROP CONSTRAINT [FK__structures__lists]
GO
ALTER TABLE [dbo].[_schemes] DROP CONSTRAINT [FK__schemes__schemes]
GO
ALTER TABLE [dbo].[_permissions] DROP CONSTRAINT [FK__permissions__users]
GO
ALTER TABLE [dbo].[_permissions] DROP CONSTRAINT [FK__permissions__roles]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [FK__objects__users2]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [FK__objects__users1]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [FK__objects__schemes]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [FK__objects__objects]
GO
ALTER TABLE [dbo].[_list_items] DROP CONSTRAINT [FK__list_items__lists]
GO
ALTER TABLE [dbo].[_list_items] DROP CONSTRAINT [FK__list_items__id_objects]
GO
ALTER TABLE [dbo].[_functions] DROP CONSTRAINT [FK__functions__schemes]
GO
ALTER TABLE [dbo].[_dependencies] DROP CONSTRAINT [FK__dependencies__schemes_2]
GO
ALTER TABLE [dbo].[_dependencies] DROP CONSTRAINT [FK__dependencies__schemes_1]
GO
ALTER TABLE [dbo].[_users] DROP CONSTRAINT [DF__users__enabled]
GO
ALTER TABLE [dbo].[_users] DROP CONSTRAINT [DF__users__date_register]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [DF__objects__date_modify]
GO
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [DF__objects__date_create]
GO
ALTER TABLE [dbo].[_deleted_objects] DROP CONSTRAINT [DF__deleted_objects__date_delete]
GO
/****** Object:  Index [IX__values_SO]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values_SO] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__structures]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__structures] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__String]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__String] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__objects]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__objects] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__Long]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__Long] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__Guid]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__Guid] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__Double]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__Double] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__DateTime]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__DateTime] ON [dbo].[_values]
GO
/****** Object:  Index [IX__values__Boolean]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__values__Boolean] ON [dbo].[_values]
GO
/****** Object:  Index [PK__values]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_values] DROP CONSTRAINT [PK__values]
GO
/****** Object:  Index [IX__users_roles__users]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__users_roles__users] ON [dbo].[_users_roles]
GO
/****** Object:  Index [IX__users_roles__roles]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__users_roles__roles] ON [dbo].[_users_roles]
GO
/****** Object:  Index [IX__users_roles]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__users_roles] ON [dbo].[_users_roles]
GO
/****** Object:  Index [PK__users_roles]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_users_roles] DROP CONSTRAINT [PK__users_roles]
GO
/****** Object:  Index [IX__users]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__users] ON [dbo].[_users]
GO
/****** Object:  Index [PK__users]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_users] DROP CONSTRAINT [PK__users]
GO
/****** Object:  Index [IX__types]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__types] ON [dbo].[_types]
GO
/****** Object:  Index [PK__types]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_types] DROP CONSTRAINT [PK__types]
GO
/****** Object:  Index [IX__structures__types]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__structures__types] ON [dbo].[_structures]
GO
/****** Object:  Index [IX__structures__structures]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__structures__structures] ON [dbo].[_structures]
GO
/****** Object:  Index [IX__structures__schemes]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__structures__schemes] ON [dbo].[_structures]
GO
/****** Object:  Index [IX__structures__lists]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__structures__lists] ON [dbo].[_structures]
GO
/****** Object:  Index [IX__structures]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__structures] ON [dbo].[_structures]
GO
/****** Object:  Index [PK__structure]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_structures] DROP CONSTRAINT [PK__structure]
GO
/****** Object:  Index [IX__schemes__schemes]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__schemes__schemes] ON [dbo].[_schemes]
GO
/****** Object:  Index [IX__schemes]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__schemes] ON [dbo].[_schemes]
GO
/****** Object:  Index [PK__schemes]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_schemes] DROP CONSTRAINT [PK__schemes]
GO
/****** Object:  Index [IX__roles]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__roles] ON [dbo].[_roles]
GO
/****** Object:  Index [PK__roles]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_roles] DROP CONSTRAINT [PK__roles]
GO
/****** Object:  Index [IX__permissions__users]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__permissions__users] ON [dbo].[_permissions]
GO
/****** Object:  Index [IX__permissions__roles]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__permissions__roles] ON [dbo].[_permissions]
GO
/****** Object:  Index [IX__permissions]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__permissions] ON [dbo].[_permissions]
GO
/****** Object:  Index [PK__object_permissions]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_permissions] DROP CONSTRAINT [PK__object_permissions]
GO
/****** Object:  Index [IX__objects__users2]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__users2] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__users1]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__users1] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__schemes]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__schemes] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__objects]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__objects] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__name]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__name] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__hash]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__hash] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__date_modify]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__date_modify] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__date_create]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__date_create] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__code_string]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__code_string] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__code_int]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__code_int] ON [dbo].[_objects]
GO
/****** Object:  Index [IX__objects__code_guid]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__objects__code_guid] ON [dbo].[_objects]
GO
/****** Object:  Index [PK__objects]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_objects] DROP CONSTRAINT [PK__objects]
GO
/****** Object:  Index [PK__lists]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_lists] DROP CONSTRAINT [PK__lists]
GO
/****** Object:  Index [IX__list_items__objects]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__list_items__objects] ON [dbo].[_list_items]
GO
/****** Object:  Index [IX__list_items__id_list]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__list_items__id_list] ON [dbo].[_list_items]
GO
/****** Object:  Index [PK__list_items]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_list_items] DROP CONSTRAINT [PK__list_items]
GO
/****** Object:  Index [IX__links]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__links] ON [dbo].[_links]
GO
/****** Object:  Index [PK__links]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_links] DROP CONSTRAINT [PK__links]
GO
/****** Object:  Index [IX__functions__schemes]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__functions__schemes] ON [dbo].[_functions]
GO
/****** Object:  Index [IX__functions_scheme_name]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_functions] DROP CONSTRAINT [IX__functions_scheme_name]
GO
/****** Object:  Index [PK__functions]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_functions] DROP CONSTRAINT [PK__functions]
GO
/****** Object:  Index [IX__dependencies__schemes_2]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__dependencies__schemes_2] ON [dbo].[_dependencies]
GO
/****** Object:  Index [IX__dependencies__schemes_1]    Script Date: 29.04.2024 13:21:00 ******/
DROP INDEX [IX__dependencies__schemes_1] ON [dbo].[_dependencies]
GO
/****** Object:  Index [IX__dependencies]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_dependencies] DROP CONSTRAINT [IX__dependencies]
GO
/****** Object:  Index [PK__dependencies]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_dependencies] DROP CONSTRAINT [PK__dependencies]
GO
/****** Object:  Index [PK___deleted__DED88B1C45174CE0]    Script Date: 29.04.2024 13:21:00 ******/
ALTER TABLE [dbo].[_deleted_objects] DROP CONSTRAINT [PK___deleted__DED88B1C45174CE0]
GO
/****** Object:  Table [dbo].[_values]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_values]') AND type in (N'U'))
DROP TABLE [dbo].[_values]
GO
/****** Object:  Table [dbo].[_users_roles]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_users_roles]') AND type in (N'U'))
DROP TABLE [dbo].[_users_roles]
GO
/****** Object:  Table [dbo].[_users]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_users]') AND type in (N'U'))
DROP TABLE [dbo].[_users]
GO
/****** Object:  Table [dbo].[_types]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_types]') AND type in (N'U'))
DROP TABLE [dbo].[_types]
GO
/****** Object:  Table [dbo].[_structures]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_structures]') AND type in (N'U'))
DROP TABLE [dbo].[_structures]
GO
/****** Object:  Table [dbo].[_schemes]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_schemes]') AND type in (N'U'))
DROP TABLE [dbo].[_schemes]
GO
/****** Object:  Table [dbo].[_roles]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_roles]') AND type in (N'U'))
DROP TABLE [dbo].[_roles]
GO
/****** Object:  Table [dbo].[_permissions]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_permissions]') AND type in (N'U'))
DROP TABLE [dbo].[_permissions]
GO
/****** Object:  Table [dbo].[_objects]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_objects]') AND type in (N'U'))
DROP TABLE [dbo].[_objects]
GO
/****** Object:  Table [dbo].[_lists]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_lists]') AND type in (N'U'))
DROP TABLE [dbo].[_lists]
GO
/****** Object:  Table [dbo].[_list_items]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_list_items]') AND type in (N'U'))
DROP TABLE [dbo].[_list_items]
GO
/****** Object:  Table [dbo].[_links]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_links]') AND type in (N'U'))
DROP TABLE [dbo].[_links]
GO
/****** Object:  Table [dbo].[_functions]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_functions]') AND type in (N'U'))
DROP TABLE [dbo].[_functions]
GO
/****** Object:  Table [dbo].[_dependencies]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_dependencies]') AND type in (N'U'))
DROP TABLE [dbo].[_dependencies]
GO
/****** Object:  Table [dbo].[_deleted_objects]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[_deleted_objects]') AND type in (N'U'))
DROP TABLE [dbo].[_deleted_objects]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 29.04.2024 13:21:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
DROP TABLE [dbo].[__EFMigrationsHistory]
GO
/****** Object:  UserDefinedFunction [dbo].[ToUTF8]    Script Date: 29.04.2024 13:21:00 ******/
DROP FUNCTION [dbo].[ToUTF8]
GO
/****** Object:  UserDefinedFunction [dbo].[ToUTF8]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create   function [dbo].[ToUTF8](@s nvarchar(max))
returns varbinary(max)
as
begin
    declare @i int = 1, @n int = datalength(@s)/2, @r varbinary(max) = 0x, @c int, @c2 int, @d varbinary(4)
    while @i <= @n
    begin
        set @c = unicode(substring(@s, @i, 1))
        if (@c & 0xFC00) = 0xD800
        begin
            set @i += 1
            if @i > @n
                return cast(cast('Malformed UTF-16 - two nchar sequence cut short' as int) as varbinary)
            set @c2 = unicode(substring(@s, @i, 1))
            if (@c2 & 0xFC00) <> 0xDC00
                return cast(cast('Malformed UTF-16 - continuation missing in a two nchar sequence' as int) as varbinary)
            set @c = (((@c & 0x3FF) * 0x400) | (@c2 & 0x3FF)) + 0x10000
        end

        if @c < 0x80
            set @d = cast(@c as binary(1))
        if @c >= 0x80 and @c < 0x800 
            set @d = cast(((@c * 4) & 0xFF00) | (@c & 0x3F) | 0xC080 as binary(2))
        if @c >= 0x800 and @c < 0x10000
            set @d = cast(((@c * 0x10) & 0xFF0000) | ((@c * 4) & 0x3F00) | (@c & 0x3F) | 0xe08080 as binary(3))
        if @c >= 0x10000
            set @d = cast(((@c * 0x40) & 0xFF000000) | ((@c * 0x10) & 0x3F0000) | ((@c * 4) & 0x3F00) | (@c & 0x3F) | 0xf0808080 as binary(4))
            
        set @r += @d
        set @i += 1
    end
    return @r
end
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](160) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_deleted_objects]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_deleted_objects](
	[_id] [bigint] NOT NULL,
	[_id_parent] [bigint] NULL,
	[_id_scheme] [bigint] NOT NULL,
	[_id_owner] [bigint] NOT NULL,
	[_id_who_change] [bigint] NOT NULL,
	[_date_create] [datetime] NOT NULL,
	[_date_modify] [datetime] NOT NULL,
	[_date_begin] [datetime] NULL,
	[_date_complete] [datetime] NULL,
	[_key] [bigint] NULL,
	[_code_int] [bigint] NULL,
	[_code_string] [nvarchar](250) NULL,
	[_code_guid] [uniqueidentifier] NULL,
	[_name] [nvarchar](250) NULL,
	[_note] [nvarchar](1000) NULL,
	[_hash] [varbinary](32) NULL,
	[_date_delete] [datetime] NOT NULL,
	[_values] [varbinary](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_dependencies]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_dependencies](
	[_id] [bigint] NOT NULL,
	[_id_scheme_1] [bigint] NULL,
	[_id_scheme_2] [bigint] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_functions]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_functions](
	[_id] [bigint] NOT NULL,
	[_id_scheme] [bigint] NOT NULL,
	[_language] [nvarchar](50) NOT NULL,
	[_name] [nvarchar](400) NOT NULL,
	[_body] [nvarchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_links]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_links](
	[_id] [bigint] NOT NULL,
	[_id_1] [bigint] NOT NULL,
	[_id_2] [bigint] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_list_items]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_list_items](
	[_id] [bigint] NOT NULL,
	[_id_list] [bigint] NOT NULL,
	[_value] [nvarchar](250) NULL,
	[_id_object] [bigint] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_lists]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_lists](
	[_id] [bigint] NOT NULL,
	[_name] [nvarchar](250) NOT NULL,
	[_alias] [nvarchar](250) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_objects]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_objects](
	[_id] [bigint] NOT NULL,
	[_id_parent] [bigint] NULL,
	[_id_scheme] [bigint] NOT NULL,
	[_id_owner] [bigint] NOT NULL,
	[_id_who_change] [bigint] NOT NULL,
	[_date_create] [datetime] NOT NULL,
	[_date_modify] [datetime] NOT NULL,
	[_date_begin] [datetime] NULL,
	[_date_complete] [datetime] NULL,
	[_key] [bigint] NULL,
	[_code_int] [bigint] NULL,
	[_code_string] [nvarchar](250) NULL,
	[_code_guid] [uniqueidentifier] NULL,
	[_name] [nvarchar](250) NULL,
	[_note] [nvarchar](1000) NULL,
	[_hash] [varbinary](32) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_permissions]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_permissions](
	[_id] [bigint] NOT NULL,
	[_id_role] [bigint] NULL,
	[_id_user] [bigint] NULL,
	[_id_ref] [bigint] NOT NULL,
	[_select] [bit] NULL,
	[_insert] [bit] NULL,
	[_update] [bit] NULL,
	[_delete] [bit] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_roles]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_roles](
	[_id] [bigint] NOT NULL,
	[_name] [nvarchar](250) NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_schemes]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_schemes](
	[_id] [bigint] NOT NULL,
	[_id_parent] [bigint] NULL,
	[_name] [nvarchar](250) NOT NULL,
	[_alias] [nvarchar](250) NULL,
	[_name_space] [nvarchar](1000) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_structures]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_structures](
	[_id] [bigint] NOT NULL,
	[_id_parent] [bigint] NULL,
	[_id_scheme] [bigint] NOT NULL,
	[_id_override] [bigint] NULL,
	[_id_type] [bigint] NOT NULL,
	[_id_list] [bigint] NULL,
	[_name] [nvarchar](250) NOT NULL,
	[_alias] [nvarchar](250) NULL,
	[_order] [bigint] NULL,
	[_readonly] [bit] NULL,
	[_allow_not_null] [bit] NULL,
	[_is_array] [bit] NULL,
	[_is_compress] [bit] NULL,
	[_store_null] [bit] NULL,
	[_default_value] [varbinary](max) NULL,
	[_default_editor] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_types]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_types](
	[_id] [bigint] NOT NULL,
	[_name] [nvarchar](250) NOT NULL,
	[_db_type] [nvarchar](250) NULL,
	[_type] [nvarchar](250) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_users]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_users](
	[_id] [bigint] NOT NULL,
	[_login] [nvarchar](250) NOT NULL,
	[_password] [nvarchar](max) NOT NULL,
	[_name] [nvarchar](250) NOT NULL,
	[_phone] [nvarchar](250) NULL,
	[_email] [nvarchar](250) NULL,
	[_date_register] [datetime] NOT NULL,
	[_date_dismiss] [datetime] NULL,
	[_enabled] [bit] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_users_roles]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_users_roles](
	[_id] [bigint] NOT NULL,
	[_id_role] [bigint] NOT NULL,
	[_id_user] [bigint] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[_values]    Script Date: 29.04.2024 13:21:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_values](
	[_id] [bigint] NOT NULL,
	[_id_structure] [bigint] NOT NULL,
	[_id_object] [bigint] NOT NULL,
	[_String] [nvarchar](850) NULL,
	[_Long] [bigint] NULL,
	[_Guid] [uniqueidentifier] NULL,
	[_Double] [float] NULL,
	[_DateTime] [datetime] NULL,
	[_Boolean] [bit] NULL,
	[_ByteArray] [varbinary](max) NULL,
	[_Text] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'00000000000000_CreateRedbSchema', N'8.0.1')
GO
INSERT [dbo].[_deleted_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash], [_date_delete], [_values]) VALUES (-9223372036854769942, NULL, -9223372036854769999, -9223372036854775800, -9223372036854775800, CAST(N'8636-03-15T02:48:18.847' AS DateTime), CAST(N'8636-03-15T02:48:18.847' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'temp', NULL, NULL, CAST(N'2024-04-25T19:30:16.927' AS DateTime), 0x5B7B225F6964223A2D393232333337323033363835343736393934312C225F69645F737472756374757265223A2D393232333337323033363835343736393939362C225F69645F6F626A656374223A2D393232333337323033363835343736393934322C225F537472696E67223A22446570656E64656E63696573222C225F6E616D65223A226E616D65222C225F64625F74797065223A22537472696E67227D2C7B225F6964223A2D393232333337323033363835343736393934302C225F69645F737472756374757265223A2D393232333337323033363835343736393939352C225F69645F6F626A656374223A2D393232333337323033363835343736393934322C225F54657874223A224D2034203139206820362076202D322048203420762032205A204D20323020352048203420762032206820313620562035205A206D202D332036204820342076203220682031332E3235206320312E312030203220302E39203220322073202D302E392032202D32203220482031352076202D32206C202D332033206C203320332076202D3220682032206320322E323120302034202D312E37392034202D342073202D312E3739202D34202D34202D34205A222C225F6E616D65223A2270617468222C225F64625F74797065223A2254657874227D5D)
GO
INSERT [dbo].[_dependencies] ([_id], [_id_scheme_1], [_id_scheme_2]) VALUES (-9223372036854769947, -9223372036854769999, -9223372036854769998)
GO
INSERT [dbo].[_dependencies] ([_id], [_id_scheme_1], [_id_scheme_2]) VALUES (-9223372036854769946, -9223372036854769998, -9223372036854769997)
GO
INSERT [dbo].[_functions] ([_id], [_id_scheme], [_language], [_name], [_body]) VALUES (-9223372036854769943, -9223372036854769997, N'js', N'Objects', N'
$(() => {

    $("#ObjectList").dxTreeList({
        scrolling: {
            useNative: true,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never"
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON("/Cnt/CRObjects/GetAllObjects")
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw "Data loading error";
                    });
            }
        }),
        sorting: {
            mode: "multiple",
        },
        selection: {
            mode: "single",
        },
        columns: [{
            dataField: "id",
            caption: "id",
            fixed: true,
            cellTemplate: function (container, options) {
                let refProperties = $("<a>", { class: "propertiesToggle", text: options.data.id, id: options.data.id, type: "CRObjects" });
                refProperties.one("click", handler1);
                container.append(refProperties);
            }
        },
        {
            dataField: "name",
            caption: "Name"
        },
        {
            dataField: "parentId",
            caption: "Parent"
        }]
    });
});
')
GO
INSERT [dbo].[_functions] ([_id], [_id_scheme], [_language], [_name], [_body]) VALUES (-9223372036854769939, -9223372036854769997, N'js', N'Deleted_objects', N'
$(() => {

    $("#DeletedObjectsList").dxTreeList({
        scrolling: {
            useNative: true,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never"
            mode: "standard" // or "virtual"
        },
        dataSource: new DevExpress.data.CustomStore({
            load: function () {
                var d = $.Deferred();
                return $.getJSON("/Cnt/CRDeletedObjects/GetAllObjects")
                    .done(function (result) {
                        d.resolve(result);
                    })
                    .fail(function () {
                        throw "Data loading error";
                    });
            }
        }),
        sorting: {
            mode: "multiple",
        },
        selection: {
            mode: "single",
        },
        columns: [{
            dataField: "id",
            caption: "id",
            fixed: true,
            cellTemplate: function (container, options) {
                let refProperties = $("<a>", { class: "propertiesToggle", text: options.data.id, id: options.data.id, type: "CRDeletedObjects" });
                refProperties.one("click", handler1);
                container.append(refProperties);
            }
        },
        {
            dataField: "name",
            caption: "Name"
        },
        {
            dataField: "parentId",
            caption: "Parent"
        }]
    });
});')
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769994, NULL, -9223372036854769999, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T14:48:18.847' AS DateTime), CAST(N'2024-02-06T14:48:18.847' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp', NULL, NULL)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769993, -9223372036854769944, -9223372036854769998, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T14:50:53.753' AS DateTime), CAST(N'2024-02-06T14:50:53.753' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General', NULL, NULL)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769992, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T14:57:30.190' AS DateTime), CAST(N'2024-02-06T14:57:30.190' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Dependencies', NULL, 0xCCB2CF618D691E3AEEC2D7503D3DEE472902CB2ADA46FA1EE8A799BB7F22A983)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769991, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Lists', NULL, 0xE551375395433AB4820BDFACA0BE74E08A366B9A8ED363058F4F01E7BA8571B7)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769990, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Objects', NULL, 0x370BC8EA63E90D53F3FB771A2C46DF6BEEF966353A004A1C16D8CF86CBE3654E)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769989, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Permissions', NULL, 0x33BAF8986B2EC3603254B59AE7943E03342477C756553EE860AD76E84F7F843A)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769988, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.Global_Settings.Roles', NULL, 0x15A6771821836148093BAD374AC3281EC1BA5F7FC78F93D3D5A8BAA16E5D1079)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769987, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Schemes', NULL, 0xD8E817C26D4BB87EDC6DAABD3F8113FF010EE9E0A7C5ABFCD8BA8921253140E2)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769986, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Structures', NULL, 0x6EDCEE092E15A925C20F6D45B6EBDBF12000D9013412DB2A375D89AC1FB1F3DF)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769985, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Types', NULL, 0x78AD453B49AF9716C39D672D0C4F962C58C0AB3D9A9DF94D94AEA48E21558045)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769984, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.Global_Settings.Users', NULL, 0xE4A749CF77F57DD346E6AB942CFB18CE12CDFE4FABD0F85898A3F90E7A861257)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769983, -9223372036854769945, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.Global_Settings.Users_roles', NULL, 0xEEA4C80BAF3458361B8F5A1F7C52407B77638FAD35FB34807D0FDDD18294314C)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769982, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Values', NULL, 0xEA6E8E3E61371D4C9C416C2E4D9B58EC2308C5534D092837806379C44A1F4756)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769981, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Deleted_objects', NULL, 0x376BCFEEC0A3BDF5AC3094A177A628247E02848B472CFB9EE5978A92167C6319)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769980, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Functions', NULL, 0x96F071991464B864F226F1342396A852B0B9741B4B47B41D355A2F572FE06070)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769979, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.Links', NULL, 0x2EB329CDEC3040DF1680B456EB2478C8E605221E341899ADE45449637C305A55)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769978, -9223372036854769993, -9223372036854769997, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T15:20:32.563' AS DateTime), CAST(N'2024-02-06T15:20:32.563' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.General.List_items', NULL, 0xFB03292190E95A4B3BE283015AA78F4D8C6264E0C610636342D491FD072FD765)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769945, -9223372036854769944, -9223372036854769998, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T19:42:54.410' AS DateTime), CAST(N'2024-02-06T19:42:54.410' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar.Global_Settings', NULL, NULL)
GO
INSERT [dbo].[_objects] ([_id], [_id_parent], [_id_scheme], [_id_owner], [_id_who_change], [_date_create], [_date_modify], [_date_begin], [_date_complete], [_key], [_code_int], [_code_string], [_code_guid], [_name], [_note], [_hash]) VALUES (-9223372036854769944, -9223372036854769994, -9223372036854769998, -9223372036854775800, -9223372036854775800, CAST(N'2024-02-06T19:44:34.133' AS DateTime), CAST(N'2024-02-06T19:44:34.133' AS DateTime), NULL, NULL, NULL, NULL, NULL, NULL, N'WebApp.Sidebar', NULL, NULL)
GO
INSERT [dbo].[_roles] ([_id], [_name]) VALUES (1, N'Admin')
GO
INSERT [dbo].[_roles] ([_id], [_name]) VALUES (2, N'Debug')
GO
INSERT [dbo].[_roles] ([_id], [_name]) VALUES (3, N'Test')
GO
INSERT [dbo].[_schemes] ([_id], [_id_parent], [_name], [_alias], [_name_space]) VALUES (-9223372036854769999, NULL, N'redb.WebApp', N'redB Dashboard', NULL)
GO
INSERT [dbo].[_schemes] ([_id], [_id_parent], [_name], [_alias], [_name_space]) VALUES (-9223372036854769998, -9223372036854769999, N'redb.WebApp.Sidebar.Group', N'sidebar Group', NULL)
GO
INSERT [dbo].[_schemes] ([_id], [_id_parent], [_name], [_alias], [_name_space]) VALUES (-9223372036854769997, -9223372036854769998, N'redb.WebApp.Sidebar.Item', N'sidebar Item', NULL)
GO
INSERT [dbo].[_structures] ([_id], [_id_parent], [_id_scheme], [_id_override], [_id_type], [_id_list], [_name], [_alias], [_order], [_readonly], [_allow_not_null], [_is_array], [_is_compress], [_store_null], [_default_value], [_default_editor]) VALUES (-9223372036854769996, NULL, -9223372036854769997, NULL, -9223372036854775700, NULL, N'name', N'redb.WebApp.Sidebar.Item.name', NULL, 1, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_structures] ([_id], [_id_parent], [_id_scheme], [_id_override], [_id_type], [_id_list], [_name], [_alias], [_order], [_readonly], [_allow_not_null], [_is_array], [_is_compress], [_store_null], [_default_value], [_default_editor]) VALUES (-9223372036854769995, NULL, -9223372036854769997, NULL, -9223372036854775702, NULL, N'path', N'redb.WebApp.Sidebar.Item.path', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775709, N'Boolean', N'Boolean', N'boolean')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775708, N'DateTime', N'DateTime', N'DateTime')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775707, N'Double', N'Double', N'double')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775706, N'ListItem', N'Long', N'_RListItem')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775705, N'Guid', N'Guid', N'Guid')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775704, N'Long', N'Long', N'long')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775703, N'Object', N'Long', N'_RObject')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775702, N'Text', N'Text', N'string')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775701, N'ByteArray', N'ByteArray', N'byte[]')
GO
INSERT [dbo].[_types] ([_id], [_name], [_db_type], [_type]) VALUES (-9223372036854775700, N'String', N'String', N'string')
GO
INSERT [dbo].[_users] ([_id], [_login], [_password], [_name], [_phone], [_email], [_date_register], [_date_dismiss], [_enabled]) VALUES (-9223372036854775800, N'admin', N'', N'admin', NULL, NULL, CAST(N'2023-12-26T01:14:34.410' AS DateTime), NULL, 1)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769977, -9223372036854769996, -9223372036854769992, N'Dependencies', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769976, -9223372036854769995, -9223372036854769992, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'M 4 19 h 6 v -2 H 4 v 2 Z M 20 5 H 4 v 2 h 16 V 5 Z m -3 6 H 4 v 2 h 13.25 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 H 15 v -2 l -3 3 l 3 3 v -2 h 2 c 2.21 0 4 -1.79 4 -4 s -1.79 -4 -4 -4 Z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769975, -9223372036854769996, -9223372036854769981, N'Deleted objects', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769974, -9223372036854769995, -9223372036854769981, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 0 0 c -0.5626 0.6404 -0.491 1.6212 0.1116 2.2238 l 6.841 6.8358 l -6.8724 6.8674 c -0.6184 0.6186 -0.6184 1.6216 0 2.2402 l 0.0226 0.0226 c 0.6186 0.6186 1.6214 0.6186 2.2402 -0 l 6.8732 -6.8682 l 6.8732 6.868 c 0.6186 0.6186 1.6216 0.6186 2.2402 -0 l 0.0226 -0.0226 c 0.6186 -0.6186 0.6186 -1.6214 -0 -2.2402 l -6.8726 -6.8674 l 6.841 -6.8358 c 0.6026 -0.6026 0.674 -1.5834 0.1116 -2.2238 c -0.6162 -0.7014 -1.6858 -0.7274 -2.3356 -0.0776 l -6.8806 6.8758 l -6.8808 -6.8754 c -0.6498 -0.65 -1.7192 -0.624 -2.3354 0.0776 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769973, -9223372036854769996, -9223372036854769980, N'Functions', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769972, -9223372036854769995, -9223372036854769980, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 11.344 5.71 c 0 -0.73 0.074 -1.122 1.199 -1.122 h 1.502 v -2.717 h -2.404 c -2.886 0 -3.903 1.36 -3.903 3.646 v 1.765 h -1.8 v 2.718 h 1.8 v 8.128 h 3.601 v -8.128 h 2.403 l 0.32 -2.718 h -2.724 l 0.006 -1.572 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769971, -9223372036854769996, -9223372036854769979, N'Links', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769970, -9223372036854769995, -9223372036854769979, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 16.469 8.924 l -2.414 2.413 c -0.156 0.156 -0.408 0.156 -0.564 0 c -0.156 -0.155 -0.156 -0.408 0 -0.563 l 2.414 -2.414 c 1.175 -1.175 1.175 -3.087 0 -4.262 c -0.57 -0.569 -1.326 -0.883 -2.132 -0.883 s -1.562 0.313 -2.132 0.883 l -2.414 2.413 c -1.175 1.175 -1.175 3.087 0 4.263 c 0.288 0.288 0.624 0.511 0.997 0.662 c 0.204 0.083 0.303 0.315 0.22 0.52 c -0.171 0.422 -0.643 0.17 -0.52 0.22 c -0.473 -0.191 -0.898 -0.474 -1.262 -0.838 c -1.487 -1.485 -1.487 -3.904 0 -5.391 l 2.414 -2.413 c 0.72 -0.72 1.678 -1.117 2.696 -1.117 s 1.976 0.396 2.696 1.117 c 1.487 1.486 1.487 3.904 0.001 5.39 m -6.393 -1.099 c -0.205 -0.083 -0.437 0.016 -0.52 0.22 c -0.083 0.205 0.016 0.437 0.22 0.52 c 0.374 0.151 0.709 0.374 0.997 0.662 c 1.176 1.176 1.176 3.088 0 4.263 l -2.414 2.413 c -0.569 0.569 -1.326 0.883 -2.131 0.883 s -1.562 -0.313 -2.132 -0.883 c -1.175 -1.175 -1.175 -3.087 0 -4.262 l 2.414 -2.414 c 0.156 -0.155 0.156 -0.408 0 -0.564 c -0.156 -0.156 -0.408 -0.156 -0.564 0 l -2.414 2.414 c -1.487 1.485 -1.487 3.904 0 5.391 c 0.72 0.72 1.678 1.116 2.696 1.116 s 1.976 -0.396 2.696 -1.116 l 2.414 -2.413 c 1.487 -1.486 1.487 -3.905 0 -5.392 c -0.364 -0.365 -0.788 -0.646 -1.262 -0.838')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769969, -9223372036854769996, -9223372036854769978, N'List items', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769968, -9223372036854769995, -9223372036854769978, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 2.25 12.584 c -0.713 0 -1.292 0.578 -1.292 1.291 s 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 s -0.579 -1.291 -1.292 -1.291 z m 0 1.723 c -0.238 0 -0.43 -0.193 -0.43 -0.432 s 0.192 -0.432 0.43 -0.432 c 0.238 0 0.431 0.193 0.431 0.432 s -0.193 0.432 -0.431 0.432 z m 3.444 -7.752 h 12.916 c 0.237 0 0.431 -0.191 0.431 -0.43 s -0.193 -0.431 -0.431 -0.431 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.431 s 0.193 0.43 0.43 0.43 z m -3.444 2.153 c -0.713 0 -1.292 0.578 -1.292 1.291 c 0 0.715 0.579 1.292 1.292 1.292 c 0.713 0 1.292 -0.577 1.292 -1.292 c 0 -0.712 -0.579 -1.291 -1.292 -1.291 z m 0 1.722 c -0.238 0 -0.43 -0.192 -0.43 -0.431 c 0 -0.237 0.192 -0.43 0.43 -0.43 c 0.238 0 0.431 0.192 0.431 0.43 c 0 0.239 -0.193 0.431 -0.431 0.431 z m 16.36 -0.86 h -12.916 c -0.238 0 -0.43 0.192 -0.43 0.43 c 0 0.238 0.192 0.431 0.43 0.431 h 12.916 c 0.237 0 0.431 -0.192 0.431 -0.431 c 0 -0.238 -0.193 -0.43 -0.431 -0.43 z m 0 3.873 h -12.916 c -0.238 0 -0.43 0.193 -0.43 0.432 s 0.192 0.432 0.43 0.432 h 12.916 c 0.237 0 0.431 -0.193 0.431 -0.432 s -0.193 -0.432 -0.431 -0.432 z m -16.36 -8.61 c -0.713 0 -1.292 0.578 -1.292 1.292 c 0 0.713 0.579 1.291 1.292 1.291 c 0.713 0 1.292 -0.578 1.292 -1.291 c 0 -0.713 -0.579 -1.292 -1.292 -1.292 z m 0 1.722 c -0.238 0 -0.43 -0.191 -0.43 -0.43 s 0.192 -0.431 0.43 -0.431 c 0.238 0 0.431 0.192 0.431 0.431 s -0.193 0.43 -0.431 0.43 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769967, -9223372036854769996, -9223372036854769991, N'Lists', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769966, -9223372036854769995, -9223372036854769991, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 14 2 h -8 c -1.1 0 -1.99 0.9 -1.99 2 l -0.01 16 c 0 1.1 0.89 2 1.99 2 h 12.01 c 1.1 0 2 -0.9 2 -2 v -12 l -6 -6 z m 2 16 h -8 v -2 h 8 v 2 z m 0 -4 h -8 v -2 h 8 v 2 z m -3 -5 v -5.5 l 5.5 5.5 h -5.5 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769965, -9223372036854769996, -9223372036854769990, N'Objects', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769964, -9223372036854769995, -9223372036854769990, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'M 4 8 h 4 V 4 H 4 v 4 Z m 6 12 h 4 v -4 h -4 v 4 Z m -6 0 h 4 v -4 H 4 v 4 Z m 0 -6 h 4 v -4 H 4 v 4 Z m 6 0 h 4 v -4 h -4 v 4 Z m 6 -10 v 4 h 4 V 4 h -4 Z m -6 4 h 4 V 4 h -4 v 4 Z m 6 6 h 4 v -4 h -4 v 4 Z m 0 6 h 4 v -4 h -4 v 4 Z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769963, -9223372036854769996, -9223372036854769989, N'Permissions', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769962, -9223372036854769995, -9223372036854769989, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 12.443 9.672 c 0.203 -0.496 0.329 -1.052 0.329 -1.652 c 0 -1.969 -1.241 -3.565 -2.772 -3.565 s -2.772 1.596 -2.772 3.565 c 0 0.599 0.126 1.156 0.33 1.652 c -1.379 0.555 -2.31 1.553 -2.31 2.704 c 0 1.75 2.128 3.169 4.753 3.169 c 2.624 0 4.753 -1.419 4.753 -3.169 c -0.001 -1.151 -0.933 -2.149 -2.311 -2.704 z m -2.443 -4.425 c 1.094 0 1.98 1.242 1.98 2.773 c 0 1.531 -0.887 2.772 -1.98 2.772 s -1.98 -1.241 -1.98 -2.772 c 0 -1.531 0.886 -2.773 1.98 -2.773 z m 0 9.506 c -2.187 0 -3.96 -1.063 -3.96 -2.377 c 0 -0.854 0.757 -1.596 1.885 -2.015 c 0.508 0.745 1.245 1.224 2.076 1.224 s 1.567 -0.479 2.076 -1.224 c 1.127 0.418 1.885 1.162 1.885 2.015 c -0.001 1.313 -1.774 2.377 -3.962 2.377 z m 0 -13.862 c -5.031 0 -9.109 4.079 -9.109 9.109 c 0 5.031 4.079 9.109 9.109 9.109 c 5.031 0 9.109 -4.078 9.109 -9.109 c 0 -5.031 -4.078 -9.109 -9.109 -9.109 z m 0 17.426 c -4.593 0 -8.317 -3.725 -8.317 -8.317 c 0 -4.593 3.724 -8.317 8.317 -8.317 c 4.593 0 8.317 3.724 8.317 8.317 c 0 4.593 -3.724 8.317 -8.317 8.317 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769961, -9223372036854769996, -9223372036854769988, N'Roles', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769960, -9223372036854769995, -9223372036854769988, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769959, -9223372036854769996, -9223372036854769987, N'Schemes', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769958, -9223372036854769995, -9223372036854769987, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 18.378 1.062 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 c 0 0.309 0.25 0.559 0.559 0.559 h 13.964 v 13.964 c 0 0.309 0.25 0.559 0.559 0.559 c 0.31 0 0.56 -0.25 0.56 -0.559 v -14.523 c 0 -0.309 -0.25 -0.559 -0.56 -0.559 z m -2.234 2.234 h -14.523 c -0.309 0 -0.559 0.25 -0.559 0.559 v 14.523 c 0 0.31 0.25 0.56 0.559 0.56 h 14.523 c 0.309 0 0.559 -0.25 0.559 -0.56 v -14.523 c -0.001 -0.309 -0.251 -0.559 -0.559 -0.559 z m -0.558 13.966 c 0 0.31 -0.25 0.558 -0.56 0.558 h -12.288 c -0.309 0 -0.559 -0.248 -0.559 -0.558 v -12.29 c 0 -0.309 0.25 -0.559 0.559 -0.559 h 12.289 c 0.31 0 0.56 0.25 0.56 0.559 v 12.29 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769957, -9223372036854769996, -9223372036854769986, N'Structures', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769956, -9223372036854769995, -9223372036854769986, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 5.029 1.734 h 10.935 c 0.317 0 0.575 -0.257 0.575 -0.575 s -0.258 -0.576 -0.575 -0.576 h -10.935 c -0.318 0 -0.576 0.258 -0.576 0.576 s 0.258 0.575 0.576 0.575 z m -4.029 3.454 v 13.812 h 18.417 v -13.812 h -18.417 z m 17.266 12.661 h -16.115 v -11.511 h 16.115 v 11.511 z m -15.539 -13.813 h 14.963 c 0.317 0 0.575 -0.257 0.575 -0.576 c 0 -0.318 -0.258 -0.575 -0.575 -0.575 h -14.963 c -0.318 0 -0.576 0.257 -0.576 0.575 c 0 0.319 0.258 0.576 0.576 0.576 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769955, -9223372036854769996, -9223372036854769985, N'Types', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769954, -9223372036854769995, -9223372036854769985, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 14.68 12.621 c -0.9 0 -1.702 0.43 -2.216 1.09 l -4.549 -2.637 c 0.284 -0.691 0.284 -1.457 0 -2.146 l 4.549 -2.638 c 0.514 0.661 1.315 1.09 2.216 1.09 c 1.549 0 2.809 -1.26 2.809 -2.808 c 0 -1.548 -1.26 -2.809 -2.809 -2.809 c -1.548 0 -2.808 1.26 -2.808 2.809 c 0 0.38 0.076 0.741 0.214 1.073 l -4.55 2.638 c -0.515 -0.661 -1.316 -1.09 -2.217 -1.09 c -1.548 0 -2.808 1.26 -2.808 2.809 s 1.26 2.808 2.808 2.808 c 0.9 0 1.702 -0.43 2.217 -1.09 l 4.55 2.637 c -0.138 0.332 -0.214 0.693 -0.214 1.074 c 0 1.549 1.26 2.809 2.808 2.809 c 1.549 0 2.809 -1.26 2.809 -2.809 s -1.26 -2.81 -2.809 -2.81 m 0 -10.109 c 1.136 0 2.06 0.923 2.06 2.06 s -0.925 2.058 -2.06 2.058 s -2.059 -0.923 -2.059 -2.059 s 0.923 -2.059 2.059 -2.059 m -9.361 9.549 c -1.136 0 -2.06 -0.924 -2.06 -2.06 s 0.923 -2.059 2.06 -2.059 c 1.135 0 2.06 0.923 2.06 2.059 s -0.925 2.06 -2.06 2.06 m 9.361 5.427 c -1.136 0 -2.059 -0.922 -2.059 -2.059 s 0.923 -2.061 2.059 -2.061 s 2.06 0.924 2.06 2.061 s -0.925 2.059 -2.06 2.059 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769953, -9223372036854769996, -9223372036854769984, N'Users', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769952, -9223372036854769995, -9223372036854769984, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 16 11 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m -8 0 c 1.66 0 2.99 -1.34 2.99 -3 s -1.33 -3 -2.99 -3 c -1.66 0 -3 1.34 -3 3 s 1.34 3 3 3 z m 0 2 c -2.33 0 -7 1.17 -7 3.5 v 2.5 h 14 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z m 8 0 c -0.29 0 -0.62 0.02 -0.97 0.05 c 1.16 0.84 1.97 1.97 1.97 3.45 v 2.5 h 6 v -2.5 c 0 -2.33 -4.67 -3.5 -7 -3.5 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769951, -9223372036854769996, -9223372036854769983, N'Users roles', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769950, -9223372036854769995, -9223372036854769983, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 12 2 c 1.1 0 2 0.9 2 2 s -0.9 2 -2 2 s -2 -0.9 -2 -2 s 0.9 -2 2 -2 z m 9 7 h -6 v 13 h -2 v -6 h -2 v 6 h -2 v -13 h -6 v -2 h 18 v 2 z m -3 3 q 1 10 3 0 z m -12 0 q 2 2 -3 0 z m -2 10 q 4 -8 0 -7 z')
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769949, -9223372036854769996, -9223372036854769982, N'Values', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
GO
INSERT [dbo].[_values] ([_id], [_id_structure], [_id_object], [_String], [_Long], [_Guid], [_Double], [_DateTime], [_Boolean], [_ByteArray], [_Text]) VALUES (-9223372036854769948, -9223372036854769995, -9223372036854769982, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'm 15.396 2.292 h -10.792 c -0.212 0 -0.385 0.174 -0.385 0.386 v 14.646 c 0 0.212 0.173 0.385 0.385 0.385 h 10.792 c 0.211 0 0.385 -0.173 0.385 -0.385 v -14.647 c 0 -0.212 -0.174 -0.385 -0.385 -0.385 m -0.386 14.646 h -10.02 v -2.698 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 s -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.321 -1.089 0.771 h -1.609 v -3.083 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.518 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -3.086 h 1.609 c 0.156 0.449 0.586 0.771 1.089 0.771 c 0.638 0 1.156 -0.519 1.156 -1.156 c 0 -0.638 -0.519 -1.156 -1.156 -1.156 c -0.503 0 -0.933 0.322 -1.089 0.771 h -1.609 v -2.699 h 10.02 v 13.876 z m -7.708 -3.084 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.385 0.385 s -0.385 -0.173 -0.385 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.385 0.385 -0.385 s 0.386 0.173 0.386 0.385 s -0.173 0.385 -0.385 0.385 s -0.386 -0.173 -0.386 -0.385 m 0 -3.854 c 0 -0.212 0.173 -0.386 0.385 -0.386 s 0.385 0.174 0.385 0.386 s -0.173 0.385 -0.384 0.385 s -0.386 -0.173 -0.386 -0.385')
GO
/****** Object:  Index [PK___deleted__DED88B1C45174CE0]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_deleted_objects] ADD  CONSTRAINT [PK___deleted__DED88B1C45174CE0] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__dependencies]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_dependencies] ADD  CONSTRAINT [PK__dependencies] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__dependencies]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_dependencies] ADD  CONSTRAINT [IX__dependencies] UNIQUE NONCLUSTERED 
(
	[_id_scheme_1] ASC,
	[_id_scheme_2] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__dependencies__schemes_1]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__dependencies__schemes_1] ON [dbo].[_dependencies]
(
	[_id_scheme_1] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__dependencies__schemes_2]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__dependencies__schemes_2] ON [dbo].[_dependencies]
(
	[_id_scheme_2] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__functions]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_functions] ADD  CONSTRAINT [PK__functions] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__functions_scheme_name]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_functions] ADD  CONSTRAINT [IX__functions_scheme_name] UNIQUE NONCLUSTERED 
(
	[_id_scheme] ASC,
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__functions__schemes]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__functions__schemes] ON [dbo].[_functions]
(
	[_id_scheme] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__links]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_links] ADD  CONSTRAINT [PK__links] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__links]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__links] ON [dbo].[_links]
(
	[_id_1] ASC,
	[_id_2] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__list_items]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_list_items] ADD  CONSTRAINT [PK__list_items] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__list_items__id_list]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__list_items__id_list] ON [dbo].[_list_items]
(
	[_id_list] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__list_items__objects]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__list_items__objects] ON [dbo].[_list_items]
(
	[_id_object] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__lists]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_lists] ADD  CONSTRAINT [PK__lists] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__objects]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_objects] ADD  CONSTRAINT [PK__objects] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__code_guid]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__code_guid] ON [dbo].[_objects]
(
	[_code_guid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__code_int]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__code_int] ON [dbo].[_objects]
(
	[_code_int] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__objects__code_string]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__code_string] ON [dbo].[_objects]
(
	[_code_string] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__date_create]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__date_create] ON [dbo].[_objects]
(
	[_date_create] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__date_modify]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__date_modify] ON [dbo].[_objects]
(
	[_date_modify] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__objects__hash]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__hash] ON [dbo].[_objects]
(
	[_hash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__objects__name]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__name] ON [dbo].[_objects]
(
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__objects]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__objects] ON [dbo].[_objects]
(
	[_id_parent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__schemes]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__schemes] ON [dbo].[_objects]
(
	[_id_scheme] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__users1]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__users1] ON [dbo].[_objects]
(
	[_id_owner] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__objects__users2]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__objects__users2] ON [dbo].[_objects]
(
	[_id_who_change] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__object_permissions]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_permissions] ADD  CONSTRAINT [PK__object_permissions] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__permissions]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__permissions] ON [dbo].[_permissions]
(
	[_id_ref] ASC,
	[_select] ASC,
	[_insert] ASC,
	[_update] ASC,
	[_delete] ASC,
	[_id_role] ASC,
	[_id_user] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__permissions__roles]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__permissions__roles] ON [dbo].[_permissions]
(
	[_id_role] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__permissions__users]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__permissions__users] ON [dbo].[_permissions]
(
	[_id_user] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__roles]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_roles] ADD  CONSTRAINT [PK__roles] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__roles]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__roles] ON [dbo].[_roles]
(
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__schemes]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_schemes] ADD  CONSTRAINT [PK__schemes] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__schemes]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__schemes] ON [dbo].[_schemes]
(
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__schemes__schemes]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__schemes__schemes] ON [dbo].[_schemes]
(
	[_id_parent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__structure]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_structures] ADD  CONSTRAINT [PK__structure] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__structures]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__structures] ON [dbo].[_structures]
(
	[_id_scheme] ASC,
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__structures__lists]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__structures__lists] ON [dbo].[_structures]
(
	[_id_list] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__structures__schemes]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__structures__schemes] ON [dbo].[_structures]
(
	[_id_scheme] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__structures__structures]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__structures__structures] ON [dbo].[_structures]
(
	[_id_parent] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__structures__types]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__structures__types] ON [dbo].[_structures]
(
	[_id_type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__types]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_types] ADD  CONSTRAINT [PK__types] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__types]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__types] ON [dbo].[_types]
(
	[_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__users]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_users] ADD  CONSTRAINT [PK__users] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__users]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__users] ON [dbo].[_users]
(
	[_login] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__users_roles]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_users_roles] ADD  CONSTRAINT [PK__users_roles] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__users_roles]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__users_roles] ON [dbo].[_users_roles]
(
	[_id_role] ASC,
	[_id_user] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__users_roles__roles]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__users_roles__roles] ON [dbo].[_users_roles]
(
	[_id_role] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__users_roles__users]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__users_roles__users] ON [dbo].[_users_roles]
(
	[_id_user] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [PK__values]    Script Date: 29.04.2024 13:21:01 ******/
ALTER TABLE [dbo].[_values] ADD  CONSTRAINT [PK__values] PRIMARY KEY NONCLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__Boolean]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__Boolean] ON [dbo].[_values]
(
	[_Boolean] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__DateTime]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__DateTime] ON [dbo].[_values]
(
	[_DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__Double]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__Double] ON [dbo].[_values]
(
	[_Double] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__Guid]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__Guid] ON [dbo].[_values]
(
	[_Guid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__Long]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__Long] ON [dbo].[_values]
(
	[_Long] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__objects]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__objects] ON [dbo].[_values]
(
	[_id_object] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX__values__String]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__String] ON [dbo].[_values]
(
	[_String] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values__structures]    Script Date: 29.04.2024 13:21:01 ******/
CREATE NONCLUSTERED INDEX [IX__values__structures] ON [dbo].[_values]
(
	[_id_structure] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX__values_SO]    Script Date: 29.04.2024 13:21:01 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX__values_SO] ON [dbo].[_values]
(
	[_id_structure] ASC,
	[_id_object] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[_deleted_objects] ADD  CONSTRAINT [DF__deleted_objects__date_delete]  DEFAULT (getdate()) FOR [_date_delete]
GO
ALTER TABLE [dbo].[_objects] ADD  CONSTRAINT [DF__objects__date_create]  DEFAULT (getdate()) FOR [_date_create]
GO
ALTER TABLE [dbo].[_objects] ADD  CONSTRAINT [DF__objects__date_modify]  DEFAULT (getdate()) FOR [_date_modify]
GO
ALTER TABLE [dbo].[_users] ADD  CONSTRAINT [DF__users__date_register]  DEFAULT (getdate()) FOR [_date_register]
GO
ALTER TABLE [dbo].[_users] ADD  CONSTRAINT [DF__users__enabled]  DEFAULT ((1)) FOR [_enabled]
GO
ALTER TABLE [dbo].[_dependencies]  WITH CHECK ADD  CONSTRAINT [FK__dependencies__schemes_1] FOREIGN KEY([_id_scheme_1])
REFERENCES [dbo].[_schemes] ([_id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_dependencies] CHECK CONSTRAINT [FK__dependencies__schemes_1]
GO
ALTER TABLE [dbo].[_dependencies]  WITH CHECK ADD  CONSTRAINT [FK__dependencies__schemes_2] FOREIGN KEY([_id_scheme_2])
REFERENCES [dbo].[_schemes] ([_id])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[_dependencies] CHECK CONSTRAINT [FK__dependencies__schemes_2]
GO
ALTER TABLE [dbo].[_functions]  WITH CHECK ADD  CONSTRAINT [FK__functions__schemes] FOREIGN KEY([_id_scheme])
REFERENCES [dbo].[_schemes] ([_id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_functions] CHECK CONSTRAINT [FK__functions__schemes]
GO
ALTER TABLE [dbo].[_list_items]  WITH CHECK ADD  CONSTRAINT [FK__list_items__id_objects] FOREIGN KEY([_id_object])
REFERENCES [dbo].[_objects] ([_id])
GO
ALTER TABLE [dbo].[_list_items] CHECK CONSTRAINT [FK__list_items__id_objects]
GO
ALTER TABLE [dbo].[_list_items]  WITH CHECK ADD  CONSTRAINT [FK__list_items__lists] FOREIGN KEY([_id_list])
REFERENCES [dbo].[_lists] ([_id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_list_items] CHECK CONSTRAINT [FK__list_items__lists]
GO
ALTER TABLE [dbo].[_objects]  WITH CHECK ADD  CONSTRAINT [FK__objects__objects] FOREIGN KEY([_id_parent])
REFERENCES [dbo].[_objects] ([_id])
GO
ALTER TABLE [dbo].[_objects] CHECK CONSTRAINT [FK__objects__objects]
GO
ALTER TABLE [dbo].[_objects]  WITH CHECK ADD  CONSTRAINT [FK__objects__schemes] FOREIGN KEY([_id_scheme])
REFERENCES [dbo].[_schemes] ([_id])
GO
ALTER TABLE [dbo].[_objects] CHECK CONSTRAINT [FK__objects__schemes]
GO
ALTER TABLE [dbo].[_objects]  WITH CHECK ADD  CONSTRAINT [FK__objects__users1] FOREIGN KEY([_id_owner])
REFERENCES [dbo].[_users] ([_id])
GO
ALTER TABLE [dbo].[_objects] CHECK CONSTRAINT [FK__objects__users1]
GO
ALTER TABLE [dbo].[_objects]  WITH CHECK ADD  CONSTRAINT [FK__objects__users2] FOREIGN KEY([_id_who_change])
REFERENCES [dbo].[_users] ([_id])
GO
ALTER TABLE [dbo].[_objects] CHECK CONSTRAINT [FK__objects__users2]
GO
ALTER TABLE [dbo].[_permissions]  WITH CHECK ADD  CONSTRAINT [FK__permissions__roles] FOREIGN KEY([_id_role])
REFERENCES [dbo].[_roles] ([_id])
GO
ALTER TABLE [dbo].[_permissions] CHECK CONSTRAINT [FK__permissions__roles]
GO
ALTER TABLE [dbo].[_permissions]  WITH CHECK ADD  CONSTRAINT [FK__permissions__users] FOREIGN KEY([_id_user])
REFERENCES [dbo].[_users] ([_id])
GO
ALTER TABLE [dbo].[_permissions] CHECK CONSTRAINT [FK__permissions__users]
GO
ALTER TABLE [dbo].[_schemes]  WITH CHECK ADD  CONSTRAINT [FK__schemes__schemes] FOREIGN KEY([_id_parent])
REFERENCES [dbo].[_schemes] ([_id])
GO
ALTER TABLE [dbo].[_schemes] CHECK CONSTRAINT [FK__schemes__schemes]
GO
ALTER TABLE [dbo].[_structures]  WITH CHECK ADD  CONSTRAINT [FK__structures__lists] FOREIGN KEY([_id_list])
REFERENCES [dbo].[_lists] ([_id])
GO
ALTER TABLE [dbo].[_structures] CHECK CONSTRAINT [FK__structures__lists]
GO
ALTER TABLE [dbo].[_structures]  WITH CHECK ADD  CONSTRAINT [FK__structures__schemes] FOREIGN KEY([_id_scheme])
REFERENCES [dbo].[_schemes] ([_id])
GO
ALTER TABLE [dbo].[_structures] CHECK CONSTRAINT [FK__structures__schemes]
GO
ALTER TABLE [dbo].[_structures]  WITH CHECK ADD  CONSTRAINT [FK__structures__structures] FOREIGN KEY([_id_parent])
REFERENCES [dbo].[_structures] ([_id])
GO
ALTER TABLE [dbo].[_structures] CHECK CONSTRAINT [FK__structures__structures]
GO
ALTER TABLE [dbo].[_structures]  WITH CHECK ADD  CONSTRAINT [FK__structures__types] FOREIGN KEY([_id_type])
REFERENCES [dbo].[_types] ([_id])
GO
ALTER TABLE [dbo].[_structures] CHECK CONSTRAINT [FK__structures__types]
GO
ALTER TABLE [dbo].[_users_roles]  WITH CHECK ADD  CONSTRAINT [FK__users_roles__roles] FOREIGN KEY([_id_role])
REFERENCES [dbo].[_roles] ([_id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_users_roles] CHECK CONSTRAINT [FK__users_roles__roles]
GO
ALTER TABLE [dbo].[_users_roles]  WITH CHECK ADD  CONSTRAINT [FK__users_roles__users] FOREIGN KEY([_id_user])
REFERENCES [dbo].[_users] ([_id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_users_roles] CHECK CONSTRAINT [FK__users_roles__users]
GO
ALTER TABLE [dbo].[_values]  WITH CHECK ADD  CONSTRAINT [FK__values__objects] FOREIGN KEY([_id_object])
REFERENCES [dbo].[_objects] ([_id])
GO
ALTER TABLE [dbo].[_values] CHECK CONSTRAINT [FK__values__objects]
GO
ALTER TABLE [dbo].[_values]  WITH CHECK ADD  CONSTRAINT [FK__values__structures] FOREIGN KEY([_id_structure])
REFERENCES [dbo].[_structures] ([_id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[_values] CHECK CONSTRAINT [FK__values__structures]
GO
ALTER TABLE [dbo].[_links]  WITH CHECK ADD  CONSTRAINT [CK__links] CHECK  (([_id_1]<>[_id_2]))
GO
ALTER TABLE [dbo].[_links] CHECK CONSTRAINT [CK__links]
GO
ALTER TABLE [dbo].[_permissions]  WITH CHECK ADD  CONSTRAINT [CK__permissions_users_roles] CHECK  (([_id_role] IS NOT NULL AND [_id_user] IS NULL OR [_id_role] IS NULL AND [_id_user] IS NOT NULL))
GO
ALTER TABLE [dbo].[_permissions] CHECK CONSTRAINT [CK__permissions_users_roles]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetChildObjects]    Script Date: 29.04.2024 13:21:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE   PROCEDURE [dbo].[sp_GetChildObjects]
  @id bigint
AS
BEGIN

    --WITH  Nodes ([_id], [_id_parent], [Level]) 
    --AS (
    --    SELECT  T._id, T._id_parent, 0 AS [Level]
    --    FROM    _objects T
    --    WHERE   T.[_id] = @id

    --    UNION ALL

    --    SELECT T._id, T._id_parent, N.[Level] + 1
    --    FROM    _objects T
    --            INNER JOIN Nodes N ON N.[_id] = T._id_parent
    --)

    --SELECT _id, _id_parent
    --FROM    [_objects]
    --WHERE   [_id] IN (
    --    SELECT  TOP 100 PERCENT N.[_id] 
    --    FROM    Nodes N
    --    ORDER BY N.[Level] DESC
    --);

with cte as (
  select T.* from _objects as T
    where T._id = @id
  union all
  select T.* from _objects as T
    inner join cte as C on T._id_parent = C._Id
)
select * from cte

END
GO
/****** Object:  Trigger [dbo].[TR__objects__deleted_objects]    Script Date: 29.04.2024 13:21:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE     TRIGGER [dbo].[TR__objects__deleted_objects]
ON [dbo].[_objects]
instead of delete
AS
begin
   insert into _deleted_objects 
   select *,getdate(),dbo.ToUTF8(cast((
   select _v.*,_s._name,_t._db_type from _values _v 
   join _structures _s on _s._id = _v._id_structure
   join _types _t on _t._id = _s._id_type
   where _v._id_object=_o._id 
   for json path) as nvarchar(max))) from deleted _o
   delete from _values where _id_object in (select _id from deleted)
   delete from _objects where _id in (select _id from deleted)
end
GO
ALTER TABLE [dbo].[_objects] ENABLE TRIGGER [TR__objects__deleted_objects]
GO

USE [redb]
GO

USE [redb]
GO

/****** Object:  Sequence [dbo].[global_identity]    Script Date: 29.04.2024 13:23:20 ******/
DROP SEQUENCE [dbo].[global_identity]
GO

USE [redb]
GO

/****** Object:  Sequence [dbo].[global_identity]    Script Date: 29.04.2024 13:23:20 ******/
CREATE SEQUENCE [dbo].[global_identity] 
 AS [bigint]
 START WITH -9223372036854770000
 INCREMENT BY 1
 MINVALUE -9223372036854775808
 MAXVALUE 9223372036854775807
 NO CACHE 
GO

