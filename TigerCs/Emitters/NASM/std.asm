%include "io.inc"
section .data
prints db '%', 's', 0
printi db '%', 'i', 0
lend db 10,0
nullptr db 14, 0, 0, 0, "Null Reference", 0
nullcode db 3, 0, 0, 0
argofrptr db 21, 0, 0, 0, "Argument Out Of Range", 0
argofrcode db 4, 0, 0, 0

section .text

;externs
CEXTERN fprintf
CEXTERN scanf
CEXTERN malloc
CEXTERN free
CEXTERN get_stdin
CEXTERN get_stdout
CEXTERN getchar
;extern _get_stderr
;TODO: search for windowa

global _strcmp
global _cconcat
global _csubstring
global _cchr
global _cord
global _cprintS
global _cprintI
global _cgetchar
global _emit_error

;params: 1: error code [int]
;        0: message    [tg_nasm string]
;return: error code    [eax]
;      : error signal  [ecx]
_emit_error:
    mov EAX, [ESP + 4]
    cmp EAX, 0
    je .null_error_exit
    add EAX, 4
    push EAX
    push dword prints
    ;call _get_stderror
    call get_stdout
    push EAX

    call fprintf
    add ESP, 12

    mov EAX, [ESP + 8]
    mov ECX, 947975
    ret

    .null_error_exit:
    push dword [nullcode]
    push dword nullptr
    call _emit_error
    add ESP, 8
    ret

;params: 0: string    [tg_nasm string]
;return: void
_cprintS:
    mov EAX, [ESP + 4]
    cmp EAX, 0
    je .null_error_exit
    add EAX, 4
    push EAX
    push dword prints

    call get_stdout
    push EAX

    call fprintf
    add ESP, 12

    xor EAX, EAX
    xor ECX, ECX
    ret

    .null_error_exit:
    push dword [nullcode]
    push dword nullptr
    call _emit_error
    add ESP, 8
    ret

;params: 0: int    [int]
;return: void
_cprintI:    
    mov EAX, [ESP + 4]
    push EAX
    
    push dword printi
    
    call get_stdout
    push EAX
    
    call fprintf
    add ESP, 12
    
    xor EAX, EAX
    xor ECX, ECX
    ret

;params: void
;return: tg_nasm string
_cgetchar:
    call getchar

    cmp al, 0
    jge .noneof
    
    push dword 5
    call malloc
    add esp, 4
    
    mov ebx, eax
    
    mov ecx, 5
    mov eax, 0
    mov edi, ebx
    rep stosb
    
    mov eax, ebx
    xor ecx, ecx
    ret
    
    .noneof:
    push eax 
    push dword 6
    call malloc
    add esp, 4
    
    mov ebx, eax
    
    mov ecx, 6
    mov eax, 0
    mov edi, ebx
    rep stosb
    
    pop eax
    inc byte [ebx]
    mov [ebx + 4], al
    
    mov eax, ebx
    xor ecx, ecx
    ret
   
;params: 0: string    [tg_nasm string]
;return: string[0]    [int]
_cord:
    mov eax, [esp + 4]
    cmp eax, 0
    je .null_error_exit
    
    mov ecx, [eax]
    cmp ecx, 0
    jg .noneof
    
    mov eax, -1
    xor ecx, ecx
    ret
       
    .noneof:
    mov eax, [eax + 4]
    and eax, 0xff
    
    xor ecx, ecx
    ret
    
    .null_error_exit:
    push dword [nullcode]
    push dword nullptr
    call _emit_error
    add ESP, 8
    ret

;params: 0: int    [int]
;return: str(int)  [tg_nasm string]    
_cchr:
    mov eax, [esp + 4]
    cmp al, 0
    jl .argument_out_of_range
    jg .cont
    
    push dword 5
    call malloc
    add esp, 4
    
    mov ebx, eax
    mov edi, eax
    xor eax, eax
    mov ecx, 5
    rep stosb
    
    mov eax, ebx
    xor ecx, ecx
    ret
    
    .cont:
    cmp al, 127
    jg .argument_out_of_range
    
    push eax 
    push dword 6
    call malloc
    add esp, 4
    
    mov ebx, eax
    
    mov ecx, 6
    mov eax, 0
    mov edi, ebx
    rep stosb
    
    pop eax
    inc byte [ebx]
    mov [ebx + 4], al
    
    mov eax, ebx
    xor ecx, ecx
    ret

    .argument_out_of_range:
    push dword [argofrcode]
    push dword argofrptr
    call _emit_error
    add ESP, 8
    ret
   
;params: 2: length    [int]
;        1: strart    [int]
;        0: string    [tg_nasm string]
;return: string[start:length]    [tg_nasm string] 
_csubstring:
    mov eax, [esp + 4]
    cmp eax, 0
    je .null_error_exit
    
    mov ecx, [eax]; ecx := len(string)
    mov edx, [esp + 8]; edx := start
    cmp ecx, edx
    jle .argument_out_of_range
    
    mov esi, eax
    add esi, 4
    add esi, edx; edi := &(sting[start]) 
    
    mov ebx, [esp + 12]; ebx := length
    add ebx, edx
    cmp ecx, ebx
    jl .argument_out_of_range
    
    push esi
    
    sub ebx, edx
    add ebx, 5
    push ebx
    call malloc
    add esp, 4
    
    mov edi, eax
    add edi, 4
    
    pop esi
    mov ecx, [esp + 12]
    
    rep movsb
    mov [edi], byte 0
    
    xor ecx, ecx
    ret    
    
    .argument_out_of_range:
    push dword [argofrcode]
    push dword argofrptr
    call _emit_error
    add ESP, 8
    ret
    
    .null_error_exit:
    push dword [nullcode]
    push dword nullptr
    call _emit_error
    add ESP, 8
    ret
    
;params: 1: string2    [tg_nasm string]
;        2: string1    [tg_nasm string]
;return: string1 + string2    [tg_nasm string]    
_cconcat:

    mov eax, [esp + 4]
    cmp eax, 0
    je .null_error_exit
    mov edx, [esp + 8]
    je .null_error_exit
    
    mov ecx, [eax]
    mov ebx, [edx]
    
    cmp ecx, 0
    jne .cont
    push ebx
    push dword 0
    push edx
    call _csubstring
    add esp, 12
    ret
    
    .cont:
    cmp ebx, 0
    jne .cont2
    push ecx
    push dword 0
    push eax
    call _csubstring
    add esp, 12
    ret
    
    .cont2:
    add ecx, ebx
    add ecx, 5
    push ecx
    call malloc
    pop ecx
    
    sub ecx, 5
    mov [eax], ecx
    mov edi, eax
    add edi, 4
    
    mov esi, [esp + 4]
    mov ecx, [esi]
    add esi, 4
    
    rep movsb
    
    mov esi, [esp + 8]
    mov ecx, [esi]
    add esi, 4
    
    rep movsb
    
    mov [edi], byte 0
    
    xor ecx, ecx
    ret

    .null_error_exit:
    push dword [nullcode]
    push dword nullptr
    call _emit_error
    add ESP, 8
    ret
    
;params: 1: string2    [tg_nasm string]
;        0: string1    [tg_nasm string]
;return: 
;        string1 = null /\ string2 = null => 0
;        string1 = null => -1
;        string2 = null =>  1
;        string1 = string2 =>  0
;        string1 < string2 => -1
;        string1 > string2 =>  1
_strcmp:
    mov eax, [esp + 4]
    mov edx, [esp + 8]
    
    cmp eax, edx
    je _strcmp_equal 
    
    cmp eax, 0
    je _strcmp_lesser   

    cmp edx, 0
    je _strcmp_greater    

    mov esi, eax
	add esi, 4
    mov eax, [eax]
    
    mov edi, edx
	add edi, 4
    mov edx, [edx]
    
    xor ecx, ecx
    _strcmp_loop:
        cmp ecx, eax
        jg _strcmp_lesser
        cmp ecx, edx
        jg _strcmp_greater
        
        mov bl, [esi]
        mov bh, [edi]
        cmp bl, bh
        jl _strcmp_lesser
        jg _strcmp_greater
        cmp bl, 0
        je _strcmp_equal
        
        inc esi
        inc edi
        inc ecx
        jmp _strcmp_loop   
    
    _strcmp_equal:
    xor eax, eax
    xor ecx, ecx
    ret
        
    _strcmp_greater:        
    mov eax, 1
    xor ecx, ecx
    ret
    
    _strcmp_lesser:
    mov eax, -1
    xor ecx, ecx
    ret