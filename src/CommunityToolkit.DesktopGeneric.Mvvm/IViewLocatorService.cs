﻿using System.ComponentModel;

namespace CommunityToolkit.DesktopGeneric.Mvvm;

public interface IViewLocatorService<TView>
    where TView : class
{
    TView CreateView<TViewModel>(TViewModel viewModel)
        where TViewModel : class, INotifyPropertyChanged;

    void Register<TViewModel>(Func<TView> viewFactory)
        where TViewModel : class, INotifyPropertyChanged;
}
