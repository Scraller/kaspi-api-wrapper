import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import Header from '@/components/layout/Header'
import { Providers } from '@/lib/providers'

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
  title: 'Kaspi Price Tracker',
  description: 'Track product prices on Kaspi.kz and get alerts when prices drop',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <Providers>
          <Header />
          <main className="min-h-screen">
            {children}
          </main>
          <footer className="border-t bg-gray-50 py-8">
            <div className="container mx-auto px-4 text-center text-sm text-gray-600">
              <p>&copy; {new Date().getFullYear()} Kaspi Price Tracker. All rights reserved.</p>
            </div>
          </footer>
        </Providers>
      </body>
    </html>
  )
}