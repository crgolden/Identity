CREATE TABLE [dbo].[ClientIdPRestrictions] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Provider] NVARCHAR (200) NOT NULL,
    [ClientId] INT            NOT NULL,
    CONSTRAINT [PK_ClientIdPRestrictions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientIdPRestrictions_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientIdPRestrictions_ClientId_Provider]
    ON [dbo].[ClientIdPRestrictions]([ClientId] ASC, [Provider] ASC);

