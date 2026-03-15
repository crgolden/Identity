CREATE TABLE [dbo].[ClientScopes] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Scope]    NVARCHAR (200) NOT NULL,
    [ClientId] INT            NOT NULL,
    CONSTRAINT [PK_ClientScopes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientScopes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientScopes_ClientId_Scope]
    ON [dbo].[ClientScopes]([ClientId] ASC, [Scope] ASC);

