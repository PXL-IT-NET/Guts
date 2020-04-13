SET NOCOUNT ON
DECLARE @TestRunCursor CURSOR;
DECLARE @AssignmentId int;
DECLARE @UserId int;
DECLARE @CreateDateTime datetime2
DECLARE @SourceCode nvarchar(max);
DECLARE @FilePath nvarchar(255);
DECLARE @FileContent nvarchar(max);
DECLARE @Index int;
BEGIN
    SET @TestRunCursor = CURSOR FOR
    SELECT AssignmentId, UserId, CreateDateTime, SourceCode 
	FROM dbo.TestRuns  
	ORDER BY CreateDateTime ASC

    OPEN @TestRunCursor 
    FETCH NEXT FROM @TestRunCursor 
    INTO @AssignmentId, @UserId, @CreateDateTime, @SourceCode

    WHILE @@FETCH_STATUS = 0
    BEGIN
		BEGIN TRY
		  --Split source into multiple files
		  SET @Index = PATINDEX('%///[^ /]%.%///' + CHAR(13) + CHAR(10) + '%', @SourceCode);
		  SET @SourceCode = TRIM(RIGHT(@SourceCode, LEN(@SourceCode) - @Index - 2));
		  WHILE(@Index > 0)
		  BEGIN	
			SET @Index = PATINDEX('%///' + CHAR(13) + CHAR(10) + '%', @SourceCode);
			SET @FilePath = TRIM(LEFT(@SourceCode, @Index - 1));
			SET @SourceCode = RIGHT(@SourceCode, LEN(@SourceCode) - @Index - 4);

			SET @Index = PATINDEX('%[^ /]///[^ /]%.%///' + CHAR(13) + CHAR(10) + '%', @SourceCode);
			IF(@Index > 0)
			BEGIN
				SET @FileContent = TRIM(LEFT(@SourceCode, @Index-1));
				SET @SourceCode = TRIM(RIGHT(@SourceCode, LEN(@SourceCode) - @Index - 3));
			END
			ELSE
			BEGIN
				SET @FileContent = TRIM(@SourceCode); 
			END

			--Check if same content already exists
			IF(LEN(@FileContent) > 0 AND NOT EXISTS(
				SELECT * 
				FROM dbo.SolutionFiles
				WHERE AssignmentId = @AssignmentId
					AND UserId = @UserId
					AND FilePath = @FilePath
					AND Content = @FileContent
			))
			BEGIN
				--Add the file
				INSERT INTO dbo.SolutionFiles(AssignmentId, UserId, ModifyDateTime, [FilePath], Content)
				VALUES(@AssignmentId, @UserId, @CreateDateTime, @FilePath, @FileContent);
			END

		  END
		END TRY
		BEGIN CATCH
			PRINT ERROR_MESSAGE()
		END CATCH
      FETCH NEXT FROM @TestRunCursor 
	  INTO @AssignmentId, @UserId, @CreateDateTime, @SourceCode
    END

    CLOSE @TestRunCursor ;
    DEALLOCATE @TestRunCursor;
END;

GO