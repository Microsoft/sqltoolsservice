Create Procedure [dbo].[P1]
AS
Begin
    With
        myCTE
        AS
        (
            select c1
            from T1
        )
    select c1
    from myCTE
End