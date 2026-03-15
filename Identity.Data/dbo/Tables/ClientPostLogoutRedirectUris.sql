CREATE TABLE [dbo].[ClientPostLogoutRedirectUris] (
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [PostLogoutRedirectUri] NVARCHAR (400) NOT NULL,
    [ClientId]              INT            NOT NULL,
    CONSTRAINT [PK_ClientPostLogoutRedirectUris] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientPostLogoutRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientPostLogoutRedirectUris_ClientId_PostLogoutRedirectUri]
    ON [dbo].[ClientPostLogoutRedirectUris]([ClientId] ASC, [PostLogoutRedirectUri] ASC);

