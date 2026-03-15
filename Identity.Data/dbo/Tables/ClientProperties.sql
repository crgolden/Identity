CREATE TABLE [dbo].[ClientProperties] (
    [Id]       INT             IDENTITY (1, 1) NOT NULL,
    [ClientId] INT             NOT NULL,
    [Key]      NVARCHAR (250)  NOT NULL,
    [Value]    NVARCHAR (2000) NOT NULL,
    CONSTRAINT [PK_ClientProperties] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ClientProperties_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ClientProperties_ClientId_Key]
    ON [dbo].[ClientProperties]([ClientId] ASC, [Key] ASC);

