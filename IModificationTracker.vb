Imports System

Public Interface IModificationTracker
    Inherits IDisposable

    Sub Add(ByVal ParamArray entities() As Object)
    Sub Delete(ByVal ParamArray entities() As Object)

    Sub AcceptModifications()

    Function CreateEntity(ByVal t As Type) As Object
    Function CreateEntity(Of T)() As T
End Interface
