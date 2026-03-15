CREATE TABLE [dbo].[IdentityResourceProperties] (
    [Id]                 INT             IDENTITY (1, 1) NOT NULL,
    [IdentityResourceId] INT             NOT NULL,
    [Key]                NVARCHAR (250)  NOT NULL,
    [Value]              NVARCHAR (2000) NOT NULL,
    CONSTRAINT [PK_IdentityResourceProperties] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_IdentityResourceProperties_IdentityResources_IdentityResourceId] FOREIGN KEY ([IdentityResourceId]) REFERENCES [dbo].[IdentityResources] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_IdentityResourceProperties_IdentityResourceId_Key]
    ON [dbo].[IdentityResourceProperties]([IdentityResourceId] ASC, [Key] ASC);

