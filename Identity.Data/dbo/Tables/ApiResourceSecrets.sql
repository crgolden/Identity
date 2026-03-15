CREATE TABLE [dbo].[ApiResourceSecrets] (
    [Id]            INT             IDENTITY (1, 1) NOT NULL,
    [ApiResourceId] INT             NOT NULL,
    [Description]   NVARCHAR (1000) NULL,
    [Value]         NVARCHAR (4000) NOT NULL,
    [Expiration]    DATETIME2 (7)   NULL,
    [Type]          NVARCHAR (250)  NOT NULL,
    [Created]       DATETIME2 (7)   NOT NULL,
    CONSTRAINT [PK_ApiResourceSecrets] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ApiResourceSecrets_ApiResources_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [dbo].[ApiResources] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ApiResourceSecrets_ApiResourceId]
    ON [dbo].[ApiResourceSecrets]([ApiResourceId] ASC);

