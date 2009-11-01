Public Class LinqRelation(Of T)
    Public _added As New List(Of T)
    Public _removed As New List(Of T)
    Public _host As Object

    Public Sub New(ByVal host As Object)
        _host = host
    End Sub
End Class

Public Class LinqRelationQueryable(Of T As Class, T2 As T)
    Implements IRelationQueryable(Of T)

    Class En
        Implements IEnumerator(Of T)

        Private _e As IEnumerator(Of T2)

        Public Sub New(ByVal e As IEnumerator(Of T2))
            _e = e
        End Sub

        Public ReadOnly Property Current() As T Implements System.Collections.Generic.IEnumerator(Of T).Current
            Get
                Return _e.Current
            End Get
        End Property

        Public ReadOnly Property Current1() As Object Implements System.Collections.IEnumerator.Current
            Get
                Return _e.Current
            End Get
        End Property

        Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
            Return _e.MoveNext
        End Function

        Public Sub Reset() Implements System.Collections.IEnumerator.Reset
            _e.Reset()
        End Sub

#Region " IDisposable Support "
        Private disposedValue As Boolean = False        ' Чтобы обнаружить избыточные вызовы

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: освободить другие состояния (управляемые объекты).
                End If

                ' TODO: освободить собственные состояния (неуправляемые объекты).
                ' TODO: задать большие поля как null.
            End If
            Me.disposedValue = True
        End Sub

        ' Этот код добавлен редактором Visual Basic для правильной реализации шаблона высвобождаемого класса.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Не изменяйте этот код. Разместите код очистки выше в Dispose(ByVal disposing As Boolean).
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class

    Private _q As IQueryable(Of T2)
    Private _lr As LinqRelation(Of T)

    Public Sub New(ByVal lr As LinqRelation(Of T), ByVal q As IQueryable(Of T2))
        _q = q
        _lr = lr
    End Sub

    'Public Sub New(ByVal m2m As Type, ByVal host As Object, ByVal q As IQueryable(Of T2))
    '    _q = q
    '    _lr = lr
    'End Sub

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Return New En(_q.GetEnumerator)
    End Function

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return _q.GetEnumerator
    End Function

    Public ReadOnly Property ElementType() As System.Type Implements System.Linq.IQueryable.ElementType
        Get
            Return _q.ElementType
        End Get
    End Property

    Public ReadOnly Property Expression() As System.Linq.Expressions.Expression Implements System.Linq.IQueryable.Expression
        Get
            Return _q.Expression
        End Get
    End Property

    Public ReadOnly Property Provider() As System.Linq.IQueryProvider Implements System.Linq.IQueryable.Provider
        Get
            Return _q.Provider
        End Get
    End Property

    Public Sub Add(ByVal o As T) Implements IRelationQueryable(Of T).Add
        _lr._added.Add(o)
        Dim pi = o.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public).SingleOrDefault(Function(item) item.PropertyType Is _lr._host.GetType)
        If pi IsNot Nothing Then pi.SetValue(o, _lr._host, Nothing)
    End Sub

    'Public Sub Reject() Implements IRelationQueryable(Of T).Reject
    '    _lr._added.Clear()
    '    _lr._removed.Clear()
    'End Sub

    Public Sub Remove(ByVal o As T) Implements IRelationQueryable(Of T).Remove
        _lr._removed.Add(o)
        Dim pi = o.GetType.GetProperties(Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public).SingleOrDefault(Function(item) item.PropertyType Is _lr._host.GetType)
        If pi IsNot Nothing Then pi.SetValue(o, Nothing, Nothing)
    End Sub

    Public ReadOnly Property Added() As System.Collections.Generic.List(Of T) Implements IRelationQueryable(Of T).Added
        Get
            Return _lr._added
        End Get
    End Property

    Public ReadOnly Property Removed() As System.Collections.Generic.List(Of T) Implements IRelationQueryable(Of T).Removed
        Get
            Return _lr._removed
        End Get
    End Property
End Class
