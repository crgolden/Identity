CREATE TABLE [dbo].[IdentityProviders] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Scheme]       NVARCHAR (200) NOT NULL,
    [DisplayName]  NVARCHAR (200) NULL,
    [Enabled]      BIT            NOT NULL,
    [Type]         NVARCHAR (20)  NOT NULL,
    [Properties]   NVARCHAR (MAX) NULL,
    [Created]      DATETIME2 (7)  NOT NULL,
    [Updated]      DATETIME2 (7)  NULL,
    [LastAccessed] DATETIME2 (7)  NULL,
    [NonEditable]  BIT            NOT NULL,
    CONSTRAINT [PK_IdentityProviders] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityProviders_Scheme]
    ON [dbo].[IdentityProviders]([Scheme] ASC);

