import Link from 'next/link'
import { usePathname } from 'next/navigation'

export default function Navigation() {
  const pathname = usePathname()

  const links = [
    { href: '/', label: 'Home' },
    { href: '/search', label: 'Search' },
    { href: '/dashboard', label: 'Dashboard' },
  ]

  return (
    <nav className="flex items-center space-x-6">
      {links.map((link) => (
        <Link
          key={link.href}
          href={link.href}
          className={`text-sm font-medium transition-colors hover:text-primary ${
            pathname === link.href
              ? 'text-foreground'
              : 'text-foreground/60'
          }`}
        >
          {link.label}
        </Link>
      ))}
    </nav>
  )
}
