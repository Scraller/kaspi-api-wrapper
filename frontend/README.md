# Kaspi Price Tracker Frontend

## Overview
The Kaspi Price Tracker Frontend is a web application designed to help users track product prices from the kaspi.kz website. Users can subscribe to specific products and receive alerts when the prices change, ensuring they never miss a good deal.

## Features
- **Product Search**: Users can search for products available on kaspi.kz.
- **Price Alerts**: Users can subscribe to price alerts for specific products and receive notifications when prices drop.
- **Dashboard**: A user-friendly dashboard displays the user's subscribed products along with their current price status.
- **Real-time Updates**: The application utilizes WebSocket connections to provide real-time updates on product prices.

## Architecture
The project is structured using a modular approach, with components organized into directories for better maintainability. The architecture is inspired by the Techinterview-space frontend project, ensuring a clean and efficient codebase.

## Technologies Used
- **React**: For building the user interface.
- **Next.js**: For server-side rendering and routing.
- **TypeScript**: For type safety and better development experience.
- **Tailwind CSS**: For styling the application.
- **Docker**: For containerization and easy deployment.
- **Redis**: For storing product state and managing subscriptions.

## Getting Started

### Prerequisites
- Node.js (version 14 or higher)
- Docker (for running the application in a container)

### Installation
1. Clone the repository:
   ```
   git clone https://github.com/yourusername/kaspi-price-tracker-frontend.git
   cd kaspi-price-tracker-frontend
   ```

2. Install dependencies:
   ```
   npm install
   ```

### Running the Application
To run the application locally, use the following command:
```
npm run dev
```
This will start the development server, and you can access the application at `http://localhost:3000`.

### Running with Docker
To run the application using Docker, navigate to the `docker` directory and use the following command:
```
docker-compose up
```
This will start both the frontend application and the Redis server.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.