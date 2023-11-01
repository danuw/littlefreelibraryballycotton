import React from 'react';
//import logo from './logo.svg';
//import './App.css';
import ImageUpload from './ImageUpload';
import GetBooksInfo from './GetBooksInfo';

function App() {
  return (
    <div className="App">
      <header>
        <h1>My Little Free Library</h1>
        <h2 className='locationFont'>Ballycotton</h2>
        <p>Take a book, share a book</p>
      </header>
      <main>
        <section id="location"></section>
        <section id="location">
          
          <GetBooksInfo />
        </section>
        <section id="location" className='upload'>
          <div id="map"></div>
          
          <ImageUpload />
        </section>
        <section id="about" className='upload'>
          <h2>About</h2>
          <p>This little free library was motivated by the <a href='https://www.hackster.io/contests/littlefreestemlibrary'>Little free STEM library Design Challenge</a></p>
          <p>
          <a href='https://www.hackster.io/contests/littlefreestemlibrary'>
            <img src="https://hackster.imgix.net/uploads/attachments/1601401/_gv5bemqJu8.blob?auto=format&w=1600&h=400&fit=min&dpr=1" alt="placeholder" width="80%" />
          </a>
          </p>
          <p>
            <a href='https://www.hackster.io/danuw/my-little-ballycotton-free-library-172091-87635b' title="You can browse our entry here">
            You can browse our entry here: <br /><br />            
            <img src="img/lib.jpg" alt="placeholder" width="80%" />
            </a>
          </p>
        </section>
      </main>
    </div>
  );
}

export default App;
