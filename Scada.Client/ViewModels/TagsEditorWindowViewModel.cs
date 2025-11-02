using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Scada.Client.Models;

namespace Scada.Client.ViewModels;

public class TagsEditorWindowViewModel : ViewModelBase
{
    private ObservableCollection<TagDefinition> _tags;
    private TagDefinition? _selectedTag;

    public TagsEditorWindowViewModel(ObservableCollection<TagDefinition> tags)
    {
        _tags = new ObservableCollection<TagDefinition>(tags);
        
        AddTagCommand = ReactiveCommand.Create(AddTag);
        DeleteTagCommand = ReactiveCommand.Create(
            DeleteTag, 
            this.WhenAnyValue(vm => vm.SelectedTag).Select(tag => tag != null)
        );
    }

    public ObservableCollection<TagDefinition> Tags
    {
        get => _tags;
        set => this.RaiseAndSetIfChanged(ref _tags, value);
    }

    public TagDefinition? SelectedTag
    {
        get => _selectedTag;
        set => this.RaiseAndSetIfChanged(ref _selectedTag, value);
    }

    public ReactiveCommand<Unit, Unit> AddTagCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteTagCommand { get; }

    private void AddTag()
    {
        var newTag = new TagDefinition
        {
            Name = $"NewTag{Tags.Count + 1}",
            Address = 0,
            Register = RegisterType.Holding,
            Type = DataType.UInt16,
            Scale = 1.0,
            Offset = 0.0,
            Enabled = true
        };
        Tags.Add(newTag);
        SelectedTag = newTag;
    }

    private void DeleteTag()
    {
        if (SelectedTag != null)
        {
            Tags.Remove(SelectedTag);
        }
    }
}
