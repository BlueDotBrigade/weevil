# Windows Presentation Foundation (WFP)

## `INotifyPropertyChanged` vs `DependencyObject` 

In the context of WPF and the MVVM design pattern, you have two primary options for implementing properties in your `ViewModel`:

1.	Using INotifyPropertyChanged:
	 - This is the most common approach in MVVM. It involves implementing the INotifyPropertyChanged interface and using "normal" C# properties.
	 - This approach is more straightforward and aligns well with the MVVM pattern, making your `ViewModel` more testable and decoupled from the WPF framework.
2.	Using DependencyObject and DependencyProperty:
	 - This approach is less common for `ViewModel` classes. DependencyObject and DependencyProperty are typically used in `View` classes or custom controls.
	 - While DependencyProperty provides advanced features like property value inheritance, animations, and better performance for property changes, these features are usually not necessary for `ViewModel` properties.
	 - UI Thread Dependency: DependencyObject instances have thread affinity, meaning they must be created and accessed on the UI thread. This restriction can cause issues if your `ViewModel` performs background operations, such as asynchronous data loading or processing, which are common in modern applications.
	 - Concurrency Limitations: By inheriting from DependencyObject, you limit your ability to perform concurrent operations within the `ViewModel` without encountering thread access exceptions.

Summary:

- For a `ViewModel`, it is generally better to use INotifyPropertyChanged with "normal" C# properties. This approach keeps your `ViewModel` clean, testable, and free from WPF-specific dependencies.
- Use INotifyPropertyChanged: This is the preferred approach for `ViewModel` properties in MVVM.
- Avoid DependencyObject: This is more suited for `View` classes or custom controls, not `ViewModel`.

## Model-View-ViewModel (MVVM)

### View

- The `View` should be aware of the `ViewModel`, which means the `View` will set the `DataContext` to an instance of a `ViewModel`.

### ViewModel

- A pure `ViewModel` should contain only the logic necessary to manage the data and state of the `View`, without any direct knowledge of how that data is presented or managed by the UI framework.

### Model

