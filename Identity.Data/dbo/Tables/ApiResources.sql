CREATE TABLE [dbo].[ApiResources] (
    [Id]                                  INT             IDENTITY (1, 1) NOT NULL,
    [Enabled]                             BIT             NOT NULL,
    [Name]                                NVARCHAR (200)  NOT NULL,
    [DisplayName]                         NVARCHAR (200)  NULL,
    [Description]                         NVARCHAR (1000) NULL,
    [AllowedAccessTokenSigningAlgorithms] NVARCHAR (100)  NULL,
    [ShowInDiscoveryDocument]             BIT             NOT NULL,
    [RequireResourceIndicator]            BIT             NOT NULL,
    [Created]                             DATETIME2 (7)   NOT NULL,
    [Updated]                             DATETIME2 (7)   NULL,
    [LastAccessed]                        DATETIME2 (7)   NULL,
    [NonEditable]                         BIT             NOT NULL,
    CONSTRAINT [PK_ApiResources] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiResources_Name]
    ON [dbo].[ApiResources]([Name] ASC);

