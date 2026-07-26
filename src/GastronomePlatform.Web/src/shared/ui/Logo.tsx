/**
 * Логотип «Жар-птица» — эталонная геометрия из спецификации логотипа.
 * Знак без леттеринга; размер задаётся через className (например, h-9 w-9).
 */
export function Logo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 210 210" xmlns="http://www.w3.org/2000/svg" role="img" className={className}>
      <title>GastronomePlatform</title>
      <path d="M84 42 C78 32, 86 25, 81 15" fill="none" stroke="#D9622B" strokeWidth="7" strokeLinecap="round" />
      <path d="M100 39 C94 28, 103 21, 98 9" fill="none" stroke="#D9622B" strokeWidth="7" strokeLinecap="round" />
      <path d="M116 42 C110 32, 118 25, 113 15" fill="none" stroke="#D9622B" strokeWidth="7" strokeLinecap="round" />
      <path d="M142 54 A66 66 0 1 0 165 116 L143 116" fill="none" stroke="#2A211A" strokeWidth="12" strokeLinecap="round" />
      <path d="M165 112 L165 72" fill="none" stroke="#D9622B" strokeWidth="11" strokeLinecap="round" />
      <path d="M165 78 C190 78, 190 104, 165 104" fill="none" stroke="#D9622B" strokeWidth="11" strokeLinecap="round" />
      <circle cx="113" cy="89" r="16" fill="none" stroke="#2A211A" strokeWidth="5.5" />
      <circle cx="118" cy="93" r="5.5" fill="#2A211A" />
    </svg>
  );
}
