// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEngine;

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        /// <summary>
        /// <see cref="AsmTokenKind"/> provider for Intel x86-64
        /// </summary>
        internal class X86AsmTokenKindProvider : AsmTokenKindProvider
        {
            private static readonly string[] Registers = new[]
            {
                "rax",
                "eax",
                "ax",
                "al",
                "ah",
                "rbx",
                "ebx",
                "bx",
                "bl",
                "bh",
                "rcx",
                "ecx",
                "cx",
                "cl",
                "ch",
                "rdx",
                "edx",
                "dx",
                "dl",
                "dh",
                "rsi",
                "esi",
                "si",
                "sil",
                "rdi",
                "edi",
                "di",
                "dil",
                "rbp",
                "ebp",
                "bp",
                "bpl",
                "rsp",
                "esp",
                "sp",
                "spl",
                "r8",
                "r8d",
                "r8w",
                "r8b",
                "r9",
                "r9d",
                "r9w",
                "r9b",
                "r10",
                "r10d",
                "r10w",
                "r10b",
                "r11",
                "r11d",
                "r11w",
                "r11b",
                "r12",
                "r12d",
                "r12w",
                "r12b",
                "r13",
                "r13d",
                "r13w",
                "r13b",
                "r14",
                "r14d",
                "r14w",
                "r14b",
                "r15",
                "r15d",
                "r15w",
                "r15b",
                "cs",
                "ss",
                "ds",
                "es",
                "fs",
                "gs",
                "cr0",
                "cr2",
                "cr3",
                "cr4",
                "cr8",
                "dr0",
                "dr1",
                "dr2",
                "dr3",
                "dr6",
                "dr7",
                "mm0",
                "mm1",
                "mm2",
                "mm3",
                "mm4",
                "mm5",
                "mm6",
                "mm7",
                "xmm0",
                "xmm1",
                "xmm2",
                "xmm3",
                "xmm4",
                "xmm5",
                "xmm6",
                "xmm7",
                "xmm8",
                "xmm9",
                "xmm10",
                "xmm11",
                "xmm12",
                "xmm13",
                "xmm14",
                "xmm15",
                "ymm0",
                "ymm1",
                "ymm2",
                "ymm3",
                "ymm4",
                "ymm5",
                "ymm6",
                "ymm7",
                "ymm8",
                "ymm9",
                "ymm10",
                "ymm11",
                "ymm12",
                "ymm13",
                "ymm14",
                "ymm15",
                "st",
                "st0",
                "st1",
                "st2",
                "st3",
                "st4",
                "st5",
                "st6",
                "st7",
            };

            private static readonly string[] Qualifiers = new[]
            {
                "offset",
                "xmmword",
                "dword",
                "qword",
                "byte",
                "ptr",
            };

            private static readonly string[] Instructions = new[]
            {
                "aaa",
                "aad",
                "aam",
                "aas",
                "adc",
                "adcx",
                "add",
                "adox",
                "and",
                "andn",
                "arpl",
                "bextr",
                "blsi",
                "blsmsk",
                "blsr",
                "bound",
                "bsf",
                "bsr",
                "bswap",
                "bt",
                "btc",
                "btr",
                "bts",
                "bzhi",
                "cbw",
                "cdq",
                "cdqe",
                "clac",
                "clc",
                "cld",
                "cli",
                "clts",
                "cmc",
                "cmova",
                "cmovae",
                "cmovb",
                "cmovbe",
                "cmovc",
                "cmove",
                "cmovg",
                "cmovge",
                "cmovl",
                "cmovle",
                "cmovna",
                "cmovnae",
                "cmovnb",
                "cmovnbe",
                "cmovnc",
                "cmovne",
                "cmovng",
                "cmovnge",
                "cmovnl",
                "cmovnle",
                "cmovno",
                "cmovnp",
                "cmovns",
                "cmovnz",
                "cmovo",
                "cmovp",
                "cmovpe",
                "cmovpo",
                "cmovs",
                "cmovz",
                "cmp",
                "cmps",
                "cmpsb",
                "cmpsd",
                "cmpsq",
                "cmpsw",
                "cmpxchg",
                "cmpxchg16b",
                "cmpxchg8b",
                "cpuid",
                "crc32",
                "cwd",
                "cwde",
                "daa",
                "das",
                "dec",
                "div",
                "enter",
                "hlt",
                "idiv",
                "imul",
                "in",
                "inc",
                "ins",
                "cqo",
                "insb",
                "insd",
                "insw",
                "int",
                "int1",
                "int3",
                "into",
                "invd",
                "invept",
                "invlpg",
                "invpcid",
                "invvpid",
                "iret",
                "lahf",
                "lar",
                "lds",
                "lea",
                "leave",
                "les",
                "lfs",
                "lgdt",
                "lgs",
                "lidt",
                "lldt",
                "lmsw",
                "lock",
                "lods",
                "lodsb",
                "lodsd",
                "lodsq",
                "lodsw",
                "loop",
                "loope",
                "loopne",
                "loopnz",
                "loopz",
                "lsl",
                "lss",
                "ltr",
                "lzcnt",
                "mov",
                "movbe",
                "movabs",
                "movs",
                "movsb",
                "movsd",
                "movsq",
                "movsw",
                "movsx",
                "movsxd",
                "movzx",
                "mul",
                "mulx",
                "neg",
                "nop",
                "not",
                "or",
                "out",
                "outs",
                "outsb",
                "outsd",
                "outsw",
                "pdep",
                "pext",
                "pop",
                "popa",
                "popad",
                "popcnt",
                "popf",
                "popfd",
                "prefetchw",
                "prefetchwt1",
                "push",
                "pusha",
                "pushad",
                "pushf",
                "pushfd",
                "rcl",
                "rcr",
                "rdfsbase",
                "rdgsbase",
                "rdmsr",
                "rdpmc",
                "rdrand",
                "rdseed",
                "rdtsc",
                "rdtscp",
                "rep",
                "repe",
                "repne",
                "repnz",
                "repz",
                "rex64",
                "rol",
                "ror",
                "rorx",
                "rsm",
                "sahf",
                "sal",
                "sar",
                "sarx",
                "sbb",
                "scas",
                "scasb",
                "scasd",
                "scasw",
                "seta",
                "setae",
                "setb",
                "setbe",
                "setc",
                "sete",
                "setg",
                "setge",
                "setl",
                "setle",
                "setna",
                "setnae",
                "setnb",
                "setnbe",
                "setnc",
                "setne",
                "setng",
                "setnge",
                "setnl",
                "setnle",
                "setno",
                "setnp",
                "setns",
                "setnz",
                "seto",
                "setp",
                "setpe",
                "setpo",
                "sets",
                "setz",
                "sgdt",
                "shl",
                "shld",
                "shlx",
                "shr",
                "shrd",
                "shrx",
                "sidt",
                "sldt",
                "smsw",
                "stac",
                "stc",
                "std",
                "sti",
                "stos",
                "stosb",
                "stosd",
                "stosq",
                "stosw",
                "str",
                "sub",
                "swapgs",
                "syscall",
                "sysenter",
                "sysexit",
                "sysret",
                "test",
                "tzcnt",
                "ud2",
                "verr",
                "verw",
                "vmcall",
                "vmclear",
                "vmfunc",
                "vmlaunch",
                "vmptrld",
                "vmptrst",
                "vmread",
                "vmresume",
                "vmwrite",
                "vmxoff",
                "vmxon",
                "wbinvd",
                "wrfsbase",
                "wrgsbase",
                "wrmsr",
                "xabort",
                "xacquire",
                "xadd",
                "xbegin",
                "xchg",
                "xend",
                "xgetbv",
                "xlat",
                "xlatb",
                "xor",
                "xrelease",
                "xrstor",
                "xsave",
                "xsaveopt",
                "xsetbv",
                "xtest",
            };

            private static readonly string[] CallInstructions = new[]
            {
                "call",
            };

            private static readonly string[] ReturnInstructions = new[]
            {
                "ret",
            };

            private static readonly string[] BranchInstructions = new[]
            {
                "ja",
                "jae",
                "jb",
                "jbe",
                "jc",
                "jcxz",
                "je",
                "jecxz",
                "jg",
                "jge",
                "jl",
                "jle",
                "jna",
                "jnae",
                "jnb",
                "jnbe",
                "jnc",
                "jne",
                "jng",
                "jnge",
                "jnl",
                "jnle",
                "jno",
                "jnp",
                "jns",
                "jnz",
                "jo",
                "jp",
                "jpe",
                "jpo",
                "js",
                "jz",
            };

            private static readonly string[] JumpInstructions = new[]
            {
                "jmp",
            };

            private static readonly string[] FpuInstructions = new[]
            {
                "f2xm1",
                "fabs",
                "fadd",
                "faddp",
                "fbld",
                "fbstp",
                "fchs",
                "fclex",
                "fcmovb",
                "fcmovbe",
                "fcmove",
                "fcmovnb",
                "fcmovnbe",
                "fcmovne",
                "fcmovnu",
                "fcmovu",
                "fcom",
                "fcomi",
                "fcomip",
                "fcomp",
                "fcompp",
                "fcos",
                "fdecstp",
                "fdiv",
                "fdivp",
                "fdivr",
                "fdivrp",
                "ffree",
                "fiadd",
                "ficom",
                "ficomp",
                "fidiv",
                "fidivr",
                "fild",
                "fimul",
                "fincstp",
                "finit",
                "fist",
                "fistp",
                "fisttp",
                "fisub",
                "fisubr",
                "fld1",
                "fld",
                "fldcw",
                "fldenv",
                "fldl2e",
                "fldl2t",
                "fldlg2",
                "fldln2",
                "fldpi",
                "fldz",
                "fmul",
                "fmulp",
                "fnclex",
                "fninit",
                "fnop",
                "fnsave",
                "fnstcw",
                "fnstenv",
                "fnstsw",
                "fpatan",
                "fprem1",
                "fprem",
                "fptan",
                "frndint",
                "frstor",
                "fsave",
                "fscale",
                "fsin",
                "fsincos",
                "fsqrt",
                "fst",
                "fstcw",
                "fstenv",
                "fstp",
                "fstsw",
                "fsub",
                "fsubp",
                "fsubr",
                "fsubrp",
                "ftst",
                "fucom",
                "fucomi",
                "fucomip",
                "fucomp",
                "fucompp",
                "fxam",
                "fxch",
                "fxrstor",
                "fxsave",
                "fxtract",
                "fyl2x",
                "fyl2xp1",
                "fwait",
                "wait",
            };

            private static readonly string[] SimdInstructions = new[]
            {
                "addpd",
                "addps",
                "addsd",
                "addss",
                "addsubpd",
                "addsubps",
                "aesdec",
                "aesdeclast",
                "aesenc",
                "aesenclast",
                "aesimc",
                "aeskeygenassist",
                "andnpd",
                "andnps",
                "andpd",
                "andps",
                "blendpd",
                "blendps",
                "blendvpd",
                "blendvps",
                "clflush",
                "cmpeqpd",
                "cmpeqps",
                "cmpeqsd",
                "cmpeqss",
                "cmplepd",
                "cmpleps",
                "cmplesd",
                "cmpless",
                "cmpltpd",
                "cmpltps",
                "cmpltsd",
                "cmpltss",
                "cmpneqpd",
                "cmpneqps",
                "cmpneqsd",
                "cmpneqss",
                "cmpnlepd",
                "cmpnleps",
                "cmpnlesd",
                "cmpnless",
                "cmpnltpd",
                "cmpnltps",
                "cmpnltsd",
                "cmpnltss",
                "cmpordpd",
                "cmpordps",
                "cmpordsd",
                "cmpordss",
                "cmppd",
                "cmpps",
                "cmpss",
                "cmpunordpd",
                "cmpunordps",
                "cmpunordsd",
                "cmpunordss",
                "comisd",
                "comiss",
                "cvtdq2pd",
                "cvtdq2ps",
                "cvtpd2dq",
                "cvtpd2pi",
                "cvtpd2ps",
                "cvtpi2pd",
                "cvtpi2ps",
                "cvtps2dq",
                "cvtps2pd",
                "cvtps2pi",
                "cvtsd2si",
                "cvtsd2ss",
                "cvtsi2sd",
                "cvtsi2ss",
                "cvtss2sd",
                "cvtss2si",
                "cvttpd2dq",
                "cvttpd2pi",
                "cvttps2dq",
                "cvttps2pi",
                "cvttsd2si",
                "cvttss2si",
                "divpd",
                "divps",
                "divsd",
                "divss",
                "dppd",
                "dpps",
                "emms",
                "extractps",
                "extrq",
                "femms",
                "fxrstor64",
                "fxsave64",
                "haddpd",
                "haddps",
                "hsubpd",
                "hsubps",
                "insertps",
                "insertq",
                "lddqu",
                "ldmxcsr",
                "lfence",
                "maskmovdqu",
                "maskmovq",
                "maxpd",
                "maxps",
                "maxsd",
                "maxss",
                "mfence",
                "minpd",
                "minps",
                "minsd",
                "minss",
                "monitor",
                "movapd",
                "movaps",
                "movd",
                "movddup",
                "movdq2q",
                "movdqa",
                "movdqu",
                "movhlps",
                "movhpd",
                "movhps",
                "movlhps",
                "movlpd",
                "movlps",
                "movmskpd",
                "movmskps",
                "movntdq",
                "movntdqa",
                "movnti",
                "movntpd",
                "movntps",
                "movntq",
                "movntsd",
                "movntss",
                "movq",
                "movq2dq",
                "movshdup",
                "movsldup",
                "movss",
                "movupd",
                "movups",
                "mpsadbw",
                "mulpd",
                "mulps",
                "mulsd",
                "mulss",
                "mwait",
                "orpd",
                "orps",
                "pabsb",
                "pabsd",
                "pabsw",
                "packssdw",
                "packsswb",
                "packusdw",
                "packuswb",
                "paddb",
                "paddd",
                "paddq",
                "paddsb",
                "paddsiw",
                "paddsw",
                "paddusb",
                "paddusw",
                "paddw",
                "palignr",
                "pand",
                "pandn",
                "pause",
                "paveb",
                "pavgb",
                "pavgusb",
                "pavgw",
                "pblendvb",
                "pblendw",
                "pclmulqdq",
                "pcmpeqb",
                "pcmpeqd",
                "pcmpeqq",
                "pcmpeqw",
                "pcmpestri",
                "pcmpestrm",
                "pcmpgtb",
                "pcmpgtd",
                "pcmpgtq",
                "pcmpgtw",
                "pcmpistri",
                "pcmpistrm",
                "pdistib",
                "pextrb",
                "pextrd",
                "pextrq",
                "pextrw",
                "pf2id",
                "pfacc",
                "pfadd",
                "pfcmpeq",
                "pfcmpge",
                "pfcmpgt",
                "pfmax",
                "pfmin",
                "pfmul",
                "pfrcp",
                "pfrcpit1",
                "pfrcpit2",
                "pfrsqit1",
                "pfrsqrt",
                "pfsub",
                "pfsubr",
                "phaddd",
                "phaddsw",
                "phaddw",
                "phminposuw",
                "phsubd",
                "phsubsw",
                "phsubw",
                "pi2fd",
                "pinsrb",
                "pinsrd",
                "pinsrq",
                "pinsrw",
                "pmachriw",
                "pmaddubsw",
                "pmaddwd",
                "pmagw",
                "pmaxsb",
                "pmaxsd",
                "pmaxsw",
                "pmaxub",
                "pmaxud",
                "pmaxuw",
                "pminsb",
                "pminsd",
                "pminsw",
                "pminub",
                "pminud",
                "pminuw",
                "pmovmskb",
                "pmovsxbd",
                "pmovsxbq",
                "pmovsxbw",
                "pmovsxdq",
                "pmovsxwd",
                "pmovsxwq",
                "pmovzxbd",
                "pmovzxbq",
                "pmovzxbw",
                "pmovzxdq",
                "pmovzxwd",
                "pmovzxwq",
                "pmuldq",
                "pmulhriw",
                "pmulhrsw",
                "pmulhrwa",
                "pmulhrwc",
                "pmulhuw",
                "pmulhw",
                "pmulld",
                "pmullw",
                "pmuludq",
                "pmvgezb",
                "pmvlzb",
                "pmvnzb",
                "pmvzb",
                "por",
                "prefetch",
                "prefetchnta",
                "prefetcht0",
                "prefetcht1",
                "prefetcht2",
                "psadbw",
                "pshufb",
                "pshufd",
                "pshufhw",
                "pshuflw",
                "pshufw",
                "psignb",
                "psignd",
                "psignw",
                "pslld",
                "pslldq",
                "psllq",
                "psllw",
                "psrad",
                "psraw",
                "psrld",
                "psrldq",
                "psrlq",
                "psrlw",
                "psubb",
                "psubd",
                "psubq",
                "psubsb",
                "psubsiw",
                "psubsw",
                "psubusb",
                "psubusw",
                "psubw",
                "ptest",
                "punpckhbw",
                "punpckhdq",
                "punpckhqdq",
                "punpckhwd",
                "punpcklbw",
                "punpckldq",
                "punpcklqdq",
                "punpcklwd",
                "pxor",
                "rcpps",
                "rcpss",
                "roundpd",
                "roundps",
                "roundsd",
                "roundss",
                "rsqrtps",
                "rsqrtss",
                "sfence",
                "shufpd",
                "shufps",
                "sqrtpd",
                "sqrtps",
                "sqrtsd",
                "sqrtss",
                "stmxcsr",
                "subpd",
                "subps",
                "subsd",
                "subss",
                "ucomisd",
                "ucomiss",
                "unpckhpd",
                "unpckhps",
                "unpcklpd",
                "unpcklps",
                "vaddpd",
                "vaddps",
                "vaddsd",
                "vaddss",
                "vaddsubpd",
                "vaddsubps",
                "vaesdec",
                "vaesdeclast",
                "vaesenc",
                "vaesenclast",
                "vaesimc",
                "vaeskeygenassist",
                "vandnpd",
                "vandnps",
                "vandpd",
                "vandps",
                "vblendpd",
                "vblendps",
                "vblendvpd",
                "vblendvps",
                "vbroadcastf128",
                "vbroadcasti128",
                "vbroadcastsd",
                "vbroadcastss",
                "vcmpeqpd",
                "vcmpeqps",
                "vcmpeqsd",
                "vcmpeqss",
                "vcmpfalsepd",
                "vcmpfalseps",
                "vcmpfalsesd",
                "vcmpfalsess",
                "vcmpgepd",
                "vcmpgeps",
                "vcmpgesd",
                "vcmpgess",
                "vcmpgtpd",
                "vcmpgtps",
                "vcmpgtsd",
                "vcmpgtss",
                "vcmplepd",
                "vcmpleps",
                "vcmplesd",
                "vcmpless",
                "vcmpltpd",
                "vcmpltps",
                "vcmpltsd",
                "vcmpltss",
                "vcmpneqpd",
                "vcmpneqps",
                "vcmpneqsd",
                "vcmpneqss",
                "vcmpngepd",
                "vcmpngeps",
                "vcmpngesd",
                "vcmpngess",
                "vcmpngtpd",
                "vcmpngtps",
                "vcmpngtsd",
                "vcmpngtss",
                "vcmpnlepd",
                "vcmpnleps",
                "vcmpnlesd",
                "vcmpnless",
                "vcmpnltpd",
                "vcmpnltps",
                "vcmpnltsd",
                "vcmpnltss",
                "vcmpordpd",
                "vcmpordps",
                "vcmpordsd",
                "vcmpordss",
                "vcmppd",
                "vcmpps",
                "vcmpsd",
                "vcmpss",
                "vcmptruepd",
                "vcmptrueps",
                "vcmptruesd",
                "vcmptruess",
                "vcmpunordpd",
                "vcmpunordps",
                "vcmpunordsd",
                "vcmpunordss",
                "vcomisd",
                "vcomiss",
                "vcvtdq2pd",
                "vcvtdq2ps",
                "vcvtpd2dq",
                "vcvtpd2ps",
                "vcvtph2ps",
                "vcvtps2dq",
                "vcvtps2pd",
                "vcvtps2ph",
                "vcvtsd2si",
                "vcvtsd2ss",
                "vcvtsi2sd",
                "vcvtsi2ss",
                "vcvtss2sd",
                "vcvtss2si",
                "vcvttpd2dq",
                "vcvttps2dq",
                "vcvttsd2si",
                "vcvttss2si",
                "vdivpd",
                "vdivps",
                "vdivsd",
                "vdivss",
                "vdppd",
                "vdpps",
                "vextractf128",
                "vextracti128",
                "vextractps",
                "vfmadd123pd",
                "vfmadd123ps",
                "vfmadd123sd",
                "vfmadd123ss",
                "vfmadd132pd",
                "vfmadd132ps",
                "vfmadd132sd",
                "vfmadd132ss",
                "vfmadd213pd",
                "vfmadd213ps",
                "vfmadd213sd",
                "vfmadd213ss",
                "vfmadd231pd",
                "vfmadd231ps",
                "vfmadd231sd",
                "vfmadd231ss",
                "vfmadd312pd",
                "vfmadd312ps",
                "vfmadd312sd",
                "vfmadd312ss",
                "vfmadd321pd",
                "vfmadd321ps",
                "vfmadd321sd",
                "vfmadd321ss",
                "vfmaddsub123pd",
                "vfmaddsub123ps",
                "vfmaddsub132pd",
                "vfmaddsub132ps",
                "vfmaddsub213pd",
                "vfmaddsub213ps",
                "vfmaddsub231pd",
                "vfmaddsub231ps",
                "vfmaddsub312pd",
                "vfmaddsub312ps",
                "vfmaddsub321pd",
                "vfmaddsub321ps",
                "vfmsub123pd",
                "vfmsub123ps",
                "vfmsub123sd",
                "vfmsub123ss",
                "vfmsub132pd",
                "vfmsub132ps",
                "vfmsub132sd",
                "vfmsub132ss",
                "vfmsub213pd",
                "vfmsub213ps",
                "vfmsub213sd",
                "vfmsub213ss",
                "vfmsub231pd",
                "vfmsub231ps",
                "vfmsub231sd",
                "vfmsub231ss",
                "vfmsub312pd",
                "vfmsub312ps",
                "vfmsub312sd",
                "vfmsub312ss",
                "vfmsub321pd",
                "vfmsub321ps",
                "vfmsub321sd",
                "vfmsub321ss",
                "vfmsubadd123pd",
                "vfmsubadd123ps",
                "vfmsubadd132pd",
                "vfmsubadd132ps",
                "vfmsubadd213pd",
                "vfmsubadd213ps",
                "vfmsubadd231pd",
                "vfmsubadd231ps",
                "vfmsubadd312pd",
                "vfmsubadd312ps",
                "vfmsubadd321pd",
                "vfmsubadd321ps",
                "vfnmadd123pd",
                "vfnmadd123ps",
                "vfnmadd123sd",
                "vfnmadd123ss",
                "vfnmadd132pd",
                "vfnmadd132ps",
                "vfnmadd132sd",
                "vfnmadd132ss",
                "vfnmadd213pd",
                "vfnmadd213ps",
                "vfnmadd213sd",
                "vfnmadd213ss",
                "vfnmadd231pd",
                "vfnmadd231ps",
                "vfnmadd231sd",
                "vfnmadd231ss",
                "vfnmadd312pd",
                "vfnmadd312ps",
                "vfnmadd312sd",
                "vfnmadd312ss",
                "vfnmadd321pd",
                "vfnmadd321ps",
                "vfnmadd321sd",
                "vfnmadd321ss",
                "vfnmsub123pd",
                "vfnmsub123ps",
                "vfnmsub123sd",
                "vfnmsub123ss",
                "vfnmsub132pd",
                "vfnmsub132ps",
                "vfnmsub132sd",
                "vfnmsub132ss",
                "vfnmsub213pd",
                "vfnmsub213ps",
                "vfnmsub213sd",
                "vfnmsub213ss",
                "vfnmsub231pd",
                "vfnmsub231ps",
                "vfnmsub231sd",
                "vfnmsub231ss",
                "vfnmsub312pd",
                "vfnmsub312ps",
                "vfnmsub312sd",
                "vfnmsub312ss",
                "vfnmsub321pd",
                "vfnmsub321ps",
                "vfnmsub321sd",
                "vfnmsub321ss",
                "vgatherdpd",
                "vgatherdps",
                "vgatherqpd",
                "vgatherqps",
                "vhaddpd",
                "vhaddps",
                "vhsubpd",
                "vhsubps",
                "vinsertf128",
                "vinserti128",
                "vinsertps",
                "vlddqu",
                "vldmxcsr",
                "vldqqu",
                "vmaskmovdqu",
                "vmaskmovpd",
                "vmaskmovps",
                "vmaxpd",
                "vmaxps",
                "vmaxsd",
                "vmaxss",
                "vminpd",
                "vminps",
                "vminsd",
                "vminss",
                "vmovapd",
                "vmovaps",
                "vmovd",
                "vmovddup",
                "vmovdqa",
                "vmovdqu",
                "vmovhlps",
                "vmovhpd",
                "vmovhps",
                "vmovlhps",
                "vmovlpd",
                "vmovlps",
                "vmovmskpd",
                "vmovmskps",
                "vmovntdq",
                "vmovntdqa",
                "vmovntpd",
                "vmovntps",
                "vmovntqq",
                "vmovq",
                "vmovqqa",
                "vmovqqu",
                "vmovsd",
                "vmovshdup",
                "vmovsldup",
                "vmovss",
                "vmovupd",
                "vmovups",
                "vmpsadbw",
                "vmulpd",
                "vmulps",
                "vmulsd",
                "vmulss",
                "vorpd",
                "vorps",
                "vpabsb",
                "vpabsd",
                "vpabsw",
                "vpackssdw",
                "vpacksswb",
                "vpackusdw",
                "vpackuswb",
                "vpaddb",
                "vpaddd",
                "vpaddq",
                "vpaddsb",
                "vpaddsw",
                "vpaddusb",
                "vpaddusw",
                "vpaddw",
                "vpalignr",
                "vpand",
                "vpandn",
                "vpavgb",
                "vpavgw",
                "vpblendd",
                "vpblendvb",
                "vpblendw",
                "vpbroadcastb",
                "vpbroadcastd",
                "vpbroadcastq",
                "vpbroadcastw",
                "vpclmulqdq",
                "vpcmpeqb",
                "vpcmpeqd",
                "vpcmpeqq",
                "vpcmpeqw",
                "vpcmpestri",
                "vpcmpestrm",
                "vpcmpgtb",
                "vpcmpgtd",
                "vpcmpgtq",
                "vpcmpgtw",
                "vpcmpistri",
                "vpcmpistrm",
                "vperm2f128",
                "vperm2i128",
                "vpermd",
                "vpermilpd",
                "vpermilps",
                "vpermpd",
                "vpermps",
                "vpermq",
                "vpextrb",
                "vpextrd",
                "vpextrq",
                "vpextrw",
                "vpgatherdd",
                "vpgatherdq",
                "vpgatherqd",
                "vpgatherqq",
                "vphaddd",
                "vphaddsw",
                "vphaddw",
                "vphminposuw",
                "vphsubd",
                "vphsubsw",
                "vphsubw",
                "vpinsrb",
                "vpinsrd",
                "vpinsrq",
                "vpinsrw",
                "vpmaddubsw",
                "vpmaddwd",
                "vpmaskmovd",
                "vpmaskmovq",
                "vpmaxsb",
                "vpmaxsd",
                "vpmaxsw",
                "vpmaxub",
                "vpmaxud",
                "vpmaxuw",
                "vpminsb",
                "vpminsd",
                "vpminsw",
                "vpminub",
                "vpminud",
                "vpminuw",
                "vpmovmskb",
                "vpmovsxbd",
                "vpmovsxbq",
                "vpmovsxbw",
                "vpmovsxdq",
                "vpmovsxwd",
                "vpmovsxwq",
                "vpmovzxbd",
                "vpmovzxbq",
                "vpmovzxbw",
                "vpmovzxdq",
                "vpmovzxwd",
                "vpmovzxwq",
                "vpmuldq",
                "vpmulhrsw",
                "vpmulhuw",
                "vpmulhw",
                "vpmulld",
                "vpmullw",
                "vpmuludq",
                "vpor",
                "vpsadbw",
                "vpshufb",
                "vpshufd",
                "vpshufhw",
                "vpshuflw",
                "vpsignb",
                "vpsignd",
                "vpsignw",
                "vpslld",
                "vpslldq",
                "vpsllq",
                "vpsllvd",
                "vpsllvq",
                "vpsllw",
                "vpsrad",
                "vpsravd",
                "vpsraw",
                "vpsrld",
                "vpsrldq",
                "vpsrlq",
                "vpsrlvd",
                "vpsrlvq",
                "vpsrlw",
                "vpsubb",
                "vpsubd",
                "vpsubq",
                "vpsubsb",
                "vpsubsw",
                "vpsubusb",
                "vpsubusw",
                "vpsubw",
                "vptest",
                "vpunpckhbw",
                "vpunpckhdq",
                "vpunpckhqdq",
                "vpunpckhwd",
                "vpunpcklbw",
                "vpunpckldq",
                "vpunpcklqdq",
                "vpunpcklwd",
                "vpxor",
                "vrcpps",
                "vrcpss",
                "vroundpd",
                "vroundps",
                "vroundsd",
                "vroundss",
                "vrsqrtps",
                "vrsqrtss",
                "vshufpd",
                "vshufps",
                "vsqrtpd",
                "vsqrtps",
                "vsqrtsd",
                "vsqrtss",
                "vstmxcsr",
                "vsubpd",
                "vsubps",
                "vsubsd",
                "vsubss",
                "vtestpd",
                "vtestps",
                "vucomisd",
                "vucomiss",
                "vunpckhpd",
                "vunpckhps",
                "vunpcklpd",
                "vunpcklps",
                "vxorpd",
                "vxorps",
                "vzeroall",
                "vzeroupper",
                "xorpd",
                "xorps",
                "xrstor64",
                "xrstors",
                "xrstors64",
                "xsave64",
                "xsavec",
                "xsavec64",
                "xsaveopt64",
                "xsaves",
                "xsaves64",
            };

            private X86AsmTokenKindProvider() : base(
                Registers.Length +
                Qualifiers.Length +
                Instructions.Length +
                CallInstructions.Length +
                BranchInstructions.Length +
                JumpInstructions.Length +
                ReturnInstructions.Length +
                FpuInstructions.Length +
                SimdInstructions.Length)
            {
                foreach (var register in Registers)
                {
                    AddTokenKind(register, AsmTokenKind.Register);
                }

                foreach (var instruction in Qualifiers)
                {
                    AddTokenKind(instruction, AsmTokenKind.Qualifier);
                }

                foreach (var instruction in Instructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.Instruction);
                }

                foreach (var instruction in CallInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.CallInstruction);
                }

                foreach (var instruction in BranchInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.BranchInstruction);
                }

                foreach (var instruction in JumpInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.JumpInstruction);
                }

                foreach (var instruction in ReturnInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.ReturnInstruction);
                }

                foreach (var instruction in FpuInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.Instruction);
                }

                foreach (var instruction in SimdInstructions)
                {
                    AddTokenKind(instruction, AsmTokenKind.InstructionSIMD);
                }
            }

            public static readonly X86AsmTokenKindProvider Instance = new X86AsmTokenKindProvider();

            /// <summary>
            /// Returns whether <see cref="instruction"/> is a packed, scalar, or infrastructure SIMD instruction.
            /// </summary>
            /// <remarks>
            /// Assumes that <see cref="instruction"/> is an X86 SIMD instruction.
            /// </remarks>
            public override SIMDkind SimdKind(StringSlice instruction) => instruction[instruction.Length - 2] switch
            {
                'p' => SIMDkind.Packed,
                's' => SIMDkind.Scalar,
                _   => SIMDkind.Infrastructure
            };

            public override bool RegisterEqual(string regA, string regB)
            {
                try
                {
                    var regAVal = RegisterMapping(regA);
                    var regBVal = RegisterMapping(regB);
                    return regAVal == regBVal;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private int RegisterMapping(string reg)
            {
                switch (reg)
                {
                    case "rax":
                    case "eax":
                    case "ax":
                    case "ah":
                    case "al":
                        return 0;
                    case "rbx":
                    case "ebx":
                    case "bx":
                    case "bh":
                    case "bl":
                        return 1;
                    case "rcx":
                    case "ecx":
                    case "cx":
                    case "ch":
                    case "cl":
                        return 2;
                    case "rdx":
                    case "edx":
                    case "dx":
                    case "dh":
                    case "dl":
                        return 3;
                    case "rsi":
                    case "esi":
                    case "si":
                    case "sil":
                        return 4;
                    case "rdi":
                    case "edi":
                    case "di":
                    case "dil":
                        return 5;
                    case "rsp":
                    case "esp":
                    case "sp":
                    case "spl":
                        return 6;
                    case "rbp":
                    case "ebp":
                    case "bp":
                    case "bpl":
                        return 7;
                    case "rip":
                    case "eip":
                    case "ip":
                        return 8;
                    case "r8":
                    case "r8d":
                    case "r8w":
                    case "r8b":
                        return 9;
                    case "r9":
                    case "r9d":
                    case "r9w":
                    case "r9b":
                        return 10;
                    case "r10":
                    case "r10d":
                    case "r10w":
                    case "r10b":
                        return 11;
                    case "r11":
                    case "r11d":
                    case "r11w":
                    case "r11b":
                        return 12;
                    case "r12":
                    case "r12d":
                    case "r12w":
                    case "r12b":
                        return 13;
                    case "r13":
                    case "r13d":
                    case "r13w":
                    case "r13b":
                        return 14;
                    case "r14":
                    case "r14d":
                    case "r14w":
                    case "r14b":
                        return 15;
                    case "r15":
                    case "r15d":
                    case "r15w":
                    case "r15b":
                        return 16;
                    case "cr0":
                        return 17;
                    case "cr2":
                        return 18;
                    case "cr3":
                        return 19;
                    case "cr4":
                        return 20;
                    case "cr8":
                        return 21;
                    case "dr0":
                        return 22;
                    case "dr1":
                        return 23;
                    case "dr2":
                        return 24;
                    case "dr3":
                        return 25;
                    case "dr6":
                        return 26;
                    case "dr7":
                        return 27;
                    case "mm0":
                        return 28;
                    case "mm1":
                        return 29;
                    case "mm2":
                        return 30;
                    case "mm3":
                        return 31;
                    case "mm4":
                        return 32;
                    case "mm5":
                        return 33;
                    case "mm6":
                        return 34;
                    case "mm7":
                        return 35;
                    case "xmm0":
                        return 36;
                    case "xmm1":
                        return 37;
                    case "xmm2":
                        return 38;
                    case "xmm3":
                        return 39;
                    case "xmm4":
                        return 40;
                    case "xmm5":
                        return 41;
                    case "xmm6":
                        return 42;
                    case "xmm7":
                        return 43;
                    case "xmm8":
                        return 44;
                    case "xmm9":
                        return 45;
                    case "xmm10":
                        return 46;
                    case "xmm11":
                        return 47;
                    case "xmm12":
                        return 48;
                    case "xmm13":
                        return 49;
                    case "xmm14":
                        return 50;
                    case "xmm15":
                        return 51;
                    case "ymm0":
                        return 52;
                    case "ymm1":
                        return 53;
                    case "ymm2":
                        return 54;
                    case "ymm3":
                        return 55;
                    case "ymm4":
                        return 56;
                    case "ymm5":
                        return 57;
                    case "ymm6":
                        return 58;
                    case "ymm7":
                        return 59;
                    case "ymm8":
                        return 60;
                    case "ymm9":
                        return 61;
                    case "ymm10":
                        return 62;
                    case "ymm11":
                        return 63;
                    case "ymm12":
                        return 64;
                    case "ymm13":
                        return 65;
                    case "ymm14":
                        return 66;
                    case "ymm15":
                        return 67;
                    case "st":
                        return 68;
                    case "st0":
                        return 69;
                    case "st1":
                        return 70;
                    case "st2":
                        return 71;
                    case "st3":
                        return 72;
                    case "st4":
                        return 73;
                    case "st5":
                        return 74;
                    case "st6":
                        return 75;
                    case "st7":
                        return 76;
                    case "cs":
                        return 77;
                    case "ss":
                        return 78;
                    case "ds":
                        return 79;
                    case "es":
                        return 80;
                    case "fs":
                        return 81;
                    case "gs":
                        return 82;
                    default:
                        throw new Exception($"Case for \"{reg}\" not implemented.");
                }
            }
        }
    }
}
