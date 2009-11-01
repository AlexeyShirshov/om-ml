Public Interface IRelationQueryable(Of T As Class)
    Inherits IQueryable(Of T)

    Sub Add(ByVal o As T)
    Sub Remove(ByVal o As T)
    'Sub Reject()

    ReadOnly Property Added() As List(Of T)
    ReadOnly Property Removed() As List(Of T)
End Interface
