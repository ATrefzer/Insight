library(tm)
library(wordcloud)

comments = read.csv("comments.csv", stringsAsFactors=FALSE)
#names(comments) = c("comment")

corpus = Corpus(VectorSource(comments[,1]))
corpus = tm_map(corpus, tolower)
#corpus = tm_map(corpus, PlainTextDocument)
corpus = tm_map(corpus, removePunctuation)
corpus = tm_map(corpus, removeWords, stopwords("english"))
frequencies = DocumentTermMatrix(corpus)
sparse = removeSparseTerms(frequencies, 0.99)
all = as.data.frame(as.matrix(sparse))

wordcloud(colnames(all), colSums(all))


