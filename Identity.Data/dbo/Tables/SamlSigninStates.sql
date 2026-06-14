CREATE TABLE [dbo].[SamlSigninStates] (
    [Id]                      BIGINT         IDENTITY (1, 1) NOT NULL,
    [StateId]                 UNIQUEIDENTIFIER NOT NULL,
    [SerializedState]         NVARCHAR (MAX) NOT NULL,
    [ExpiresAtUtc]            DATETIME2 (7)  NOT NULL,
    [ServiceProviderEntityId] NVARCHAR (200) NOT NULL,
    CONSTRAINT [PK_SamlSigninStates] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_SamlSigninStates_ExpiresAtUtc]
    ON [dbo].[SamlSigninStates]([ExpiresAtUtc] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlSigninStates_StateId]
    ON [dbo].[SamlSigninStates]([StateId] ASC);

