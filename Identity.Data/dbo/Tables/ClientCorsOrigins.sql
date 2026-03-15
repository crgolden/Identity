CREATE TABLE [dbo].[ClientCorsOrigins] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [Origin]   NVARCHAR (150) NOT NULL,
    [ClientId] INT            NOT NULL,
    CONSTRAINT [PK_ClientCorsOrigins] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientCorsOrigins_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientCorsOrigins_ClientId_Origin]
    ON [dbo].[ClientCorsOrigins]([ClientId] ASC, [Origin] ASC);

