USE [METERMAN]
GO
 
/****** Object:  Table [dbo].[Tabi_Data]    Script Date: 2014-09-05 11:07:44 AM ******/
SET ANSI_NULLS ON
GO
 
SET QUOTED_IDENTIFIER ON
GO
 
SET ANSI_PADDING ON
GO
 
CREATE TABLE [dbo].[Tabi_Data](
       [Agent_ID] [int] NULL,
       [Ani] [varchar](50) NULL,
       [Call_Date] [datetime] NULL,
       [Call_ID] [varchar](50) NULL,
       [Campaign] [varchar](50) NULL,
       [Comment] [varchar](900) NULL,
       [Customer_ID] [int] NULL,
       [dnis] [int] NULL,
       [Duration] [int] NULL,
       [End_Reason] [varchar](50) NULL,
       [Memo] [varchar](50) NULL,
       [Queue] [int] NULL,
       [Status_Code] [int] NULL,
       [Status_Detail] [varchar](50) NULL,
       [Status_Group] [varchar](50) NULL,
       [Status_Text] [varchar](50) NULL,
       [Firstname] [varchar](250) NULL,
       [Lastname] [varchar](250) NULL,
       [Row_ID] [int] IDENTITY(1,1) NOT NULL
) ON [PRIMARY]
 
GO
 
SET ANSI_PADDING OFF
GO