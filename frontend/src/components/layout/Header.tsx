import Link from 'next/link'
import { Button } from '@/components/ui/button'

export default function Header() {
  return (
    <header className="border-b bg-white">
      <div className="container mx-auto px-4 py-4 flex items-center justify-between">
        <Link href="/" className="text-2xl font-bold text-blue-600">
          Kaspi Price Tracker
        </Link>
        
        <nav className="flex items-center space-x-6">
          <Link href="/" className="text-gray-600 hover:text-gray-900">
            Search
          </Link>
          <Link href="/dashboard" className="text-gray-600 hover:text-gray-900">
            Dashboard
          </Link>
          <Button variant="outline" size="sm">
            Sign In
          </Button>
        </nav>
      </div>
    </header>
  )
}
