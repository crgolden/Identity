CREATE TABLE [dbo].[ApiScopeClaims] (
    [Id]      INT            IDENTITY (1, 1) NOT NULL,
    [ScopeId] INT            NOT NULL,
    [Type]    NVARCHAR (200) NOT NULL,
    CONSTRAINT [PK_ApiScopeClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ApiScopeClaims_ApiScopes_ScopeId] FOREIGN KEY ([ScopeId]) REFERENCES [dbo].[ApiScopes] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApiScopeClaims_ScopeId_Type]
    ON [dbo].[ApiScopeClaims]([ScopeId] ASC, [Type] ASC);

