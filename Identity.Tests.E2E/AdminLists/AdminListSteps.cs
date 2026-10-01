namespace Identity.Tests.E2E.AdminLists;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class AdminListSteps(BrowserScenario scenario, ListOwner owner)
{
    private ListEditor? _editor;
    private string? _key;
    private string? _changedValue;
    private int? _entryRowId;

    private ListEditor Editor => _editor ?? throw new InvalidOperationException("No list was edited in this scenario.");

    private string Key => _key ?? throw new InvalidOperationException("No entry was added in this scenario.");

    private string ChangedValue => _changedValue ?? throw new InvalidOperationException("No entry was changed in this scenario.");

    private int EntryRowId => _entryRowId ?? throw new InvalidOperationException("No entry existed before this scenario acted.");

    [Given("its {word} hold an entry")]
    public async Task GivenItsListHoldsAnEntry(string collection)
    {
        await AddEntryAsync(collection);
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(Editor.DetailsUrl);
        _entryRowId = await Editor.RowIdByKey(scenario.Fixture, Key);
        await Assertions.Expect(scenario.Page.Locator($"{Editor.RowIdPrefix}{EntryRowId}")).ToBeVisibleAsync();
    }

    [When("they add an entry to its {word}")]
    public Task WhenTheyAddAnEntryToItsList(string collection) => AddEntryAsync(collection);

    [When("they remove that entry")]
    public async Task WhenTheyRemoveThatEntry()
    {
        await OpenEditorAsync();
        await scenario.Page.ClickAsync(Editor.RemoveButtonId);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [When("they change that entry")]
    public async Task WhenTheyChangeThatEntry()
    {
        _changedValue = $"e2e-updated-{Guid.NewGuid():N}";
        await OpenEditorAsync();
        await scenario.Page.FillAsync(Editor.ChangedFieldId, ChangedValue);
        await scenario.Page.ClickAsync("#save-submit");
    }

    [Then("its {word} show the new entry")]
    public async Task ThenItsListShowsTheNewEntry(string collection)
    {
        Assert.Equal(collection, Editor.Collection);
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(Editor.DetailsUrl);
        var rowId = await Editor.RowIdByKey(scenario.Fixture, Key);
        await Assertions.Expect(scenario.Page.Locator($"{Editor.RowIdPrefix}{rowId}")).ToBeVisibleAsync();
    }

    [Then("its {word} no longer show the entry")]
    public async Task ThenItsListNoLongerShowsTheEntry(string collection)
    {
        Assert.Equal(collection, Editor.Collection);
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(Editor.DetailsUrl);
        await Assertions.Expect(scenario.Page.Locator($"{Editor.RowIdPrefix}{EntryRowId}")).ToHaveCountAsync(0);
    }

    [Then("its {word} show the changed entry")]
    public async Task ThenItsListShowsTheChangedEntry(string collection)
    {
        Assert.Equal(collection, Editor.Collection);
        await Assertions.Expect(scenario.Page).ToHaveURLAsync(Editor.DetailsUrl);
        var rowId = await Editor.RowIdByChangedValue(scenario.Fixture, ChangedValue);
        await Assertions.Expect(scenario.Page.Locator($"{Editor.RowIdPrefix}{rowId}")).ToBeVisibleAsync();
    }

    private Task<IResponse?> OpenEditorAsync() =>
        owner.Kind == ListOwnerKind.Client
            ? scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{Editor.Section}/Edit/{Editor.Collection}?id={owner.Id}")
            : scenario.Page.GotoAsync($"{AuthorizationNames.AdminFolder}/{Editor.Section}/Edit/{Editor.Collection}/{owner.Id}");

    private async Task AddEntryAsync(string collection)
    {
        _editor = ListEditor.For(owner.Kind, collection);
        _key = Editor.NewKey();
        await OpenEditorAsync();
        await scenario.Page.ClickAsync("#btn-add-row");
        await scenario.Page.FillAsync(Editor.KeyFieldId, Key);
        if (Editor.CompanionFieldId is { } companionFieldId && Editor.NewCompanionValue is { } newCompanionValue)
        {
            await scenario.Page.FillAsync(companionFieldId, newCompanionValue());
        }

        await scenario.Page.ClickAsync("#save-submit");
    }
}
