#include <stdio.h>
#include <stdlib.h>

FILE* get_stdout()
{
	return stdout;
}

FILE* get_stdin()
{
	return stdin;
}

FILE* get_stderr()
{
	return stderr;
}