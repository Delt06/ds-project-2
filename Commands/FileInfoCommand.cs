﻿using System;

namespace Commands
{
	[Serializable]
	public sealed class FileInfoCommand : ICommand
	{
		public int Id { get; set; }

		public FileInfoCommand(int id) => Id = id;

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"FileInfo ID={Id}";
	}
}