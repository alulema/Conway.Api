using Conway.Api.DataAccess;
using Conway.Api.Exceptions;
using Conway.Api.Models;
using Conway.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Conway.Api.Tests.Services;

[TestFixture]
    public class GameOfLifeServiceTests
    {
        private Mock<GameOfLifeContext> _mockContext;
        private Mock<ILogger<GameOfLifeService>> _mockLogger;
        private GameOfLifeService _service;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<GameOfLifeContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _mockContext = new Mock<GameOfLifeContext>(options);
            _service = new GameOfLifeService(_mockContext.Object, new NullLogger<GameOfLifeService>());
        }

        [Test]
        public async Task UploadNewBoardAsync_ValidState_ReturnsBoardId()
        {
            // Arrange
            var initialState = new bool[5, 5];
            initialState[2, 2] = true;
            _mockContext.Setup(ctx => ctx.Boards
                .AddAsync(It.IsAny<GameBoard>(), It.IsAny<CancellationToken>()));

            // Act
            var boardId = await _service.UploadNewBoardAsync(initialState);

            // Assert
            boardId.Should().NotBeEmpty();
        }

        [Test]
        public void UploadNewBoardAsync_InvalidState_ThrowsInvalidBoardStateException()
        {
            // Arrange
            var invalidInitialState = new bool[0, 0];

            // Act
            Func<Task> act = async () => await _service.UploadNewBoardAsync(invalidInitialState);

            // Assert
            act.Should().ThrowAsync<InvalidBoardStateException>();
        }

        [Test]
        public async Task GetNextStateAsync_BoardExists_ReturnsNextState()
        {
            // Arrange
            var initialState = new[,]
            {
                { false, true, false },
                { false, true, false },
                { false, true, false }
            };
            var boardId = Guid.NewGuid();
            var board = new GameBoard { Id = boardId, State = initialState };
            _mockContext.Setup(ctx => ctx.Boards.FindAsync(boardId))
                        .ReturnsAsync(board);

            // Act
            var nextState = await _service.GetNextStateAsync(boardId);

            // Assert
            nextState.Should().NotBeNull();
        }

        [Test]
        public void GetNextStateAsync_BoardDoesNotExist_ThrowsBoardNotFoundException()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service.GetNextStateAsync(boardId);

            // Assert
            act.Should().ThrowAsync<BoardNotFoundException>();
        }

        [Test]
        public async Task GetStateAfterXGenerationsAsync_ValidGenerations_ReturnsCorrectState()
        {
            // Arrange
            var initialState = new[,]
            {
                { false, true, false },
                { false, true, false },
                { false, true, false }
            };
            var boardId = Guid.NewGuid();
            var board = new GameBoard { Id = boardId, State = initialState };
            _mockContext.Setup(ctx => ctx.Boards.FindAsync(boardId))
                        .ReturnsAsync(board);

            // Act
            var stateAfterGenerations = await _service.GetStateAfterXGenerationsAsync(boardId, 1);

            // Assert
            stateAfterGenerations.Should().NotBeNull();
        }

        [Test]
        public async Task GetFinalStateAsync_ReachesStableState_ReturnsStableState()
        {
            // Arrange
            var initialState = new[,]
            {
                { false, false, false, false },
                { false, true, true, false },
                { false, true, true, false },
                { false, false, false, false }
            };
            var boardId = Guid.NewGuid();
            var board = new GameBoard { Id = boardId, State = initialState };
            _mockContext.Setup(ctx => ctx.Boards.FindAsync(boardId))
                        .ReturnsAsync(board);

            // Act
            var finalState = await _service.GetFinalStateAsync(boardId, 10);

            // Assert
            finalState.Should().NotBeNull();
        }

        [Test]
        public void GetFinalStateAsync_NeverReachesStableState_ThrowsException()
        {
            // Arrange
            var initialState = new[,]
            {
                { true, true },
                { true, true }
            };
            var boardId = Guid.NewGuid();
            var board = new GameBoard { Id = boardId, State = initialState };
            _mockContext.Setup(ctx => ctx.Boards.FindAsync(boardId))
                        .ReturnsAsync(board);

            // Act
            Func<Task> act = async () => await _service.GetFinalStateAsync(boardId, 1);

            // Assert
            act.Should().ThrowAsync<Exception>();
        }
    }