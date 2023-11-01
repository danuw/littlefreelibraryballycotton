import React, { useState, useEffect } from 'react';

const GetBooksInfo: React.FC = () => {
    const [books, setBooks] = useState([]);

    useEffect(() => {
        fetch('/api/GetBooksInfo')
            .then(response => response.json())
            .then(data => setBooks(data.books));
    }, []);

    return (
        <div>
            <h2>Latest reported books in the library</h2>
            <ul>
                {books.map((book:any, index) => (
                    <li key={index}>
                        <h3>{book.name}</h3>
                        <p>Author: {book.author}</p>
                        <p>Tags: {book.tags}</p>
                        <p>Summary: {book.summary}</p>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default GetBooksInfo;