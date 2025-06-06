﻿// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Unit tests do not require localization.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "DOM008", Justification = "No need to implement `OnPropertyChanged` because this is handled by the PostSharp `NotifyPropertyChanged` aspect.")]