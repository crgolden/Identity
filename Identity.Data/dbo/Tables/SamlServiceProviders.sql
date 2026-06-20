CREATE TABLE [dbo].[SamlServiceProviders] (
    [Id]                          INT              IDENTITY (1, 1) NOT NULL,
    [EntityId]                    NVARCHAR (200)   NOT NULL,
    [DisplayName]                 NVARCHAR (200)   NULL,
    [Description]                 NVARCHAR (1000)  NULL,
    [Enabled]                     BIT              NOT NULL,
    [ClockSkewSeconds]            FLOAT (53)       NULL,
    [RequestMaxAgeSeconds]        FLOAT (53)       NULL,
    [AssertionLifetimeSeconds]    FLOAT (53)       NULL,
    [SingleLogoutServiceLocation] NVARCHAR (400)   NULL,
    [SingleLogoutServiceBinding]  INT              NULL,
    [RequireSignedAuthnRequests]  BIT              NULL,
    [RequireSignedLogoutResponses] BIT             NULL,
    [AllowIdpInitiated]           BIT              NOT NULL,
    [DefaultNameIdFormat]         NVARCHAR (2000)  NULL,
    [EmailNameIdClaimType]        NVARCHAR (200)   NULL,
    [SigningBehavior]             INT              NULL,
    [AllowedSignatureAlgorithms]  NVARCHAR (MAX)   NULL,
    [Created]                     DATETIME2 (7)    NOT NULL,
    [Updated]                     DATETIME2 (7)    NULL,
    [LastAccessed]                DATETIME2 (7)    NULL,
    [NonEditable]                 BIT              NOT NULL,
    CONSTRAINT [PK_SamlServiceProviders] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlServiceProviders_EntityId]
    ON [dbo].[SamlServiceProviders]([EntityId] ASC);

