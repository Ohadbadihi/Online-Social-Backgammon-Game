import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import { Suspense, lazy } from 'react';
import { NotificationProvider } from './Tools/NotificationContext';
import { PlayerProvider } from './Tools/GamePlayersContext';
import { ErrorProvider } from './Tools/ErrorContext';
import ProtectedRoute from './route/ProtectedRoute';
import './App.css';
import { UserProvider } from './Tools/UserContext';
import './route/spinner.css';

const Login = lazy(() => import('./Components/login/form/FormLogin'));
const Home = lazy(() => import('./Components/home/Home'));
const Game = lazy(() => import('./Components/game/Game'));
const ErrorPage = lazy(() => import('./Components/ErrorPage/ErrorPage'));

function App() {
  return (
  
      <NotificationProvider>
        <PlayerProvider>
          <UserProvider>
            <Suspense fallback={<div className="spinner"></div>}>
              <Router>
              <ErrorProvider>
                <div className='app-container'>

                  <Routes>
                    <Route path="/login" element={<Login />} />
                    <Route path="/home" element={<ProtectedRoute> <Home /> </ProtectedRoute>} />
                    <Route path="/game" element={<ProtectedRoute> <Game /> </ProtectedRoute>} />
                    <Route path="/error" element={<ErrorPage />} />
                    <Route path="*" element={<Login />} />
                  </Routes>

                </div>
                </ErrorProvider>
              </Router>
            </Suspense>
          </UserProvider>
        </PlayerProvider>
      </NotificationProvider>
    
  );
}
export default App;
