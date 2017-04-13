#include <stdio.h>
#include <stdlib.h>
#define linemaxsize = 512

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

char* cgetline()
{
	char stor[512 + 4];
	
	int* count = (int*)stor;
	*count = 0;
	while((*count) < 512 + 4)
	{
		char a = getchar();
		if(a == '\n' || a == '\0')break;
		stor[(*count) + 4] = a;
		*count = (*count) + 1;
	}

	char* ret = (char*)malloc(sizeof(char)* (*count) + 5);
	int i = 0;
	while (i < (*count) + 4)
	{
		ret[i] = stor[i];
		i++;
	}
	ret[i] = '\0';
	return ret;
}
