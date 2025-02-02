import './homeBody.css';
function HomeBody() {

    return (
        <div className="body-container">
            <div className="content-box">
                <div className="content-item">
                    <h3>How to Play Backgammon</h3>
                    <iframe className='videoContent' src="https://www.youtube.com/embed/xXE5AwzNQ2s" title="How to Play Backgammon" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin">
                    </iframe>
                    <p>Learn the basic rules and strategies of backgammon in this tutorial video.</p>
                </div>
                <div className="content-item">
                    <h3>Advanced Tactics</h3>
                    <iframe className='videoContent' src="https://www.youtube.com/embed/yyrSJFJsOMA" title="Backgammon Core Strategies - BackgammonHQ" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen>
                    </iframe>
                    <p>Master advanced strategies to increase your win rate in backgammon.</p>
                </div>
                <div className="content-item">
                    <h3>Beginner's Mistakes</h3>
                    <iframe className='videoContent' src="https://www.youtube.com/embed/EuqQA5GoBHQ" title="Beginner&#39;s Mistakes in Backgammon - Lesson 1 of 12" loading="lazy" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen>
                    </iframe>
                    <p>Learn common mistakes to increase your win rate in backgammon.</p>
                </div>

                <div className="content-item">
                    <h3>Advanced Tactics</h3>
                    <iframe className='videoContent' src="https://www.youtube.com/embed/SDJxSTdbEpc" title="How To Play Opening Moves in Backgammon" loading="lazy" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen>
                    </iframe>
                    <p>How To Play Opening Moves in Backgammon to increase your win rate in backgammon.</p>
                </div>
                <div className="content-item">
                    <h3>Advanced Tactics</h3>
                    <video controls>
                        <source src="path-to-another-video.mp4" type="video/mp4" />
                    </video>
                    <p>Master advanced strategies to increase your win rate in backgammon.</p>
                </div>
                <div className="content-item">
                    <h3>Advanced Tactics</h3>
                    <video controls>
                        <source src="path-to-another-video.mp4" type="video/mp4" />
                    </video>
                    <p>Master advanced strategies to increase your win rate in backgammon.</p>
                </div>
            </div>
        </div>
    )
}

export default HomeBody;