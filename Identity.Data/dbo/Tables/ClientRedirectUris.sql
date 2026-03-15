CREATE TABLE [dbo].[ClientRedirectUris] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [RedirectUri] NVARCHAR (400) NOT NULL,
    [ClientId]    INT            NOT NULL,
    CONSTRAINT [PK_ClientRedirectUris] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientRedirectUris_ClientId_RedirectUri]
    ON [dbo].[ClientRedirectUris]([ClientId] ASC, [RedirectUri] ASC);

